/*
*	AStarAdHocPathfinder (AHP)
*   Copyright (C) 2014 Mark Rabenhorst and contributors
*
*   This program is free software; you can redistribute it and/or modify
*   it under the terms of the GNU General Public License as published by
*   the Free Software Foundation; either version 2 of the License, or
*   (at your option) any later version.
*
*   This program is distributed in the hope that it will be useful,
*   but WITHOUT ANY WARRANTY; without even the implied warranty of
*   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*   GNU General Public License for more details.
*
*   You should have received a copy of the GNU General Public License along
*   with this program; if not, write to the Free Software Foundation, Inc.,
*   51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/


using UnityEngine;
using System;
using System.Collections;

public class AStarAdHocPathfinder : MonoBehaviour {
	
	// Position storage (Gameobject overrides Vector3)
	public GameObject startObject;
	public GameObject targetObject;
	public Vector3 start;
	public Vector3 target;
	
	// These hold the closest node vector which is rounded 
	// by the resolution of the Navigation
	// (1.23, 4.56, 7.89) with resolution of 0.5 would be: (1.0, 4.5, 8.0)
	private Vector3 closestStartNodeVector;
	private Vector3 closestTargetNodeVector;
	
	// Last node in path or null
	private AStarAdHocPathfinderNode currentNode = null;
	
	// Navigation grid resolution (ideally 1.0)
	public float navResolution = 1.0f;
	
	// Barriers and restrictions
	public float maxAngle = 45.0f;	
	public string[] outOfBoundsTags;
	private Hashtable OOBTags;
	
	// Timeout for runaway pathfinding
	public float searchTimeout = 10.0f;
	
	// Enable to use Vector distance instead of manhattan cube as the heuristic
	public bool useOptimalHeuristics = false;

	// List storage
	private BinaryHeap openList;
	private Hashtable closedList;
	
	// Raycast Info
	public int raycastHeight = 1000;
	
	// Storage for preset position Vectors
	private Vector3 TLVector;
	private Vector3 TCVector;
	private Vector3 TRVector;
	private Vector3 MLVector;
	private Vector3 MRVector;
	private Vector3 BLVector;
	private Vector3 BCVector;
	private Vector3 BRVector;
	
	// Flag and result storage
	public bool finishedPathfinding = false;
	public AStarAdHocPathfinderNode firstNode = null;
	
	// Cycle information
	public int cyclesPerFrame = 2;
	private int currentCycle = 0;
	
	// Debug Objects/Storage
	public bool debugMode = false;
	public GameObject debugOpenListObject = null;
	public GameObject debugClosedListObject = null;
	public GameObject debugPathObject = null;
	private ArrayList debugObjects = null;
	
	// Debug timing
	private float estimatedSearchTime = 0.0f;
	
	// Debug GUI output
	public GUIText debugTextStatus;
	
	
	// Use this for initialization
	void Awake( ) {
		debugObjects = new ArrayList();
	}
	
	private void PrepareToFindPathToTarget() {
		// Prepare open/closed list
		openList = new BinaryHeap();
		closedList = new Hashtable();
		
		// Predetermine the 8 surrounding node positions to prevent recalculation
		TLVector = new Vector3(-navResolution, 0, navResolution);
		TCVector = new Vector3( 0,     		   0, navResolution);
		TRVector = new Vector3( navResolution, 0, navResolution);
		MLVector = new Vector3(-navResolution, 0, 0);
		MRVector = new Vector3( navResolution, 0, 0);
		BLVector = new Vector3(-navResolution, 0, -navResolution);
		BCVector = new Vector3( 0,             0, -navResolution);
		BRVector = new Vector3( navResolution, 0, -navResolution);
		
		// Prepare debug/status timer values
		estimatedSearchTime = 0.0f;
		
		// Reset cycle monitor
		currentCycle = 0;
	
		// Declare hashtable for OOB object tags for fast lookup
		OOBTags = new Hashtable();
		foreach( string tag in outOfBoundsTags ) {
			OOBTags[tag] = 1;
		}
	}
	
	// Use this to hault a pathfinder
	public void Reset( ) {
		finishedPathfinding = false;
		StopCoroutine( "FindPathToTargetCorutine" ); 
	}
	
	// Use this to start a pathfinder after setting the target
	public void FindPathToTarget( ) {
		StartCoroutine( "FindPathToTargetCorutine" );
	}
	
	
	private void DidNotFindPath( ) {
		finishedPathfinding = true;
		firstNode = null;
	}
	
	private void FoundPath( AStarAdHocPathfinderNode startingNode ) {
		finishedPathfinding = true;
		firstNode = startingNode;
	}
	
	
	public IEnumerator FindPathToTargetCorutine( ) {
		if( debugMode ) {
			UpdateDebugText("Finding Path...");
			RemoveAllDebugObjects();
		}
		
		// Last minute initializations
		PrepareToFindPathToTarget();
		
		// Initialize timers
		float beginPathSearchTime = Time.realtimeSinceStartup;
		float beginTimeout = Time.realtimeSinceStartup;
		
		// Game objects override vectors for start and target
		if( startObject ) {
			start = startObject.transform.position;
		}
		
		// Target must currently be deactivated to prevent raycast interference
		if( targetObject ) {
			target = targetObject.transform.position;
			targetObject.SetActive(false);
		}
		
		// Get rounded Vector closest to start
		var tmpStart = start * (1.0f / navResolution);
		closestStartNodeVector = new Vector3( Mathf.Round(tmpStart.x), Mathf.Round(tmpStart.y), Mathf.Round(tmpStart.z) );
		closestStartNodeVector /= (1.0f / navResolution);
		
		// Get rounded Vector closest to target
		var tmpTarget = target * (1.0f / navResolution);
		closestTargetNodeVector = new Vector3( Mathf.Round(tmpTarget.x), Mathf.Round(tmpTarget.y), Mathf.Round(tmpTarget.z) );
		closestTargetNodeVector /= (1.0f / navResolution);
		
		// Check to make sure that the last node is reachable
		if( !IsWalkable( target ) || !IsWalkable( closestTargetNodeVector ) ) {
			UpdateDebugText("Invalid target: not reachable");
			Debug.Log("Invalid target: not reachable");
			
			DidNotFindPath();
		}
		
		// Assign first node
		AStarAdHocPathfinderNode firstNode = new AStarAdHocPathfinderNode();
		firstNode.position = closestStartNodeVector;
		firstNode.originCost = 0;
		firstNode.destinCost = EstimatedCostToVector( firstNode.position, closestTargetNodeVector );
		firstNode.weight = firstNode.originCost + firstNode.destinCost;
		firstNode.hashCode = ConstructNodeHashCode(firstNode.position);
		
		// Add it to the open list
		openList.Add( firstNode );
		
		
		// Start the search
		while( ((BinaryHeap)openList).Count() > 0 ) {
			
			// Check to make sure pathfinder has cycles left for this frame
			if( currentCycle == cyclesPerFrame-1 ) {
				// If not, yield to main thread
				currentCycle = 0;
				yield return null;
			} else {
				// Increase cycle count
				currentCycle++;
			}
			
			// Check on timeout to prevent runaway pathfinding
			if( searchTimeout != 0 && Time.realtimeSinceStartup - beginTimeout >= searchTimeout ) {
				Debug.Log("Navigation Timed out at " + searchTimeout + " seconds");
				UpdateDebugText("Navigation Timed out at " + searchTimeout + " seconds");
				DidNotFindPath();
			}
			
			// Check to see if the current node is the closestTargetNode
			// This indicates that a path has been found successfully
			if( currentNode != null && Vector3.Scale( currentNode.position, new Vector3( 1, 0, 1 ) ) == Vector3.Scale( closestTargetNodeVector, new Vector3( 1, 0, 1) ) ) {
				break;
			}
			
			// Find smallest weight in openList and set to current node
			currentNode = openList.Remove( );	
			
			// Add it to the closed list
			closedList[currentNode.hashCode] = currentNode;
			
			
			// Create list of all possible NodeHashCodes ajacent to current node
			Vector3[] ajacentNodeVectorList = new Vector3[8];
			ajacentNodeVectorList[0] = TLVector + currentNode.position;
			ajacentNodeVectorList[1] = TCVector + currentNode.position;
			ajacentNodeVectorList[2] = TRVector + currentNode.position;
			ajacentNodeVectorList[3] = MLVector + currentNode.position;
			ajacentNodeVectorList[4] = MRVector + currentNode.position;
			ajacentNodeVectorList[5] = BLVector + currentNode.position;
			ajacentNodeVectorList[6] = BCVector + currentNode.position;
			ajacentNodeVectorList[7] = BRVector + currentNode.position;
			
			// Loop through the ajacent nodes
			foreach( Vector3 nodeVector in ajacentNodeVectorList ) {
				
				// Create node hash code for lookup				
				string nodeHashCode = ConstructNodeHashCode( nodeVector );
				
				// Ignore if on closed list or if it isn't walkable
				float yHit = -Mathf.Infinity;
				if( closedList[nodeHashCode] != null || !IsWalkable(currentNode.position, nodeVector, ref yHit) ) {
					continue;
				}
				
				// Otherwise, update the y axis of the node to the raycast hit y
				Vector3 nodeVectorCorrected = new Vector3( nodeVector.x, yHit, nodeVector.z ); // Update y of nodeVector to be the terrain surface
				
				
				// Calculate weights for use in the next conditions
				float nodeOriginCost = currentNode.originCost + ActualCostToVector( currentNode.position, nodeVectorCorrected );
				float nodeDestinCost;
				
				// Optimal heuristics uses straight line distance rather than manhattan cube
				if( useOptimalHeuristics ) {
					nodeDestinCost = ActualCostToVector( nodeVectorCorrected, closestTargetNodeVector );
				} else {
					nodeDestinCost = EstimatedCostToVector( nodeVectorCorrected, closestTargetNodeVector );
				}
						
				// Check to see if this node is already on the open list
				AStarAdHocPathfinderNode existingNode = ((BinaryHeap)openList).NodeWithHashCode(nodeHashCode);
				
				// This is a crucial debugging block to check for desyncronous HashIndex-Heap relationships. This will
				// cause an immediate termination as the pathfinder is messed up somehow. In theory, this should never
				// happen. But may occur at some time during edits
				if( existingNode != null && existingNode.hashCode != nodeHashCode ) {
					Debug.Log ("Hash Table Desync - Aborting!");
					DidNotFindPath();
					return null;
				}

				// If the node already exists in the open list and this is a faster way to get to the node,
				// update the node's origin cost to reflect the faster pathway
				if( existingNode != null && existingNode.originCost > nodeOriginCost ) {
					existingNode.originCost = nodeOriginCost;
					existingNode.UpdateWeight();
					existingNode.parent = currentNode;
					((BinaryHeap)openList).Update(existingNode);
				} else if( existingNode == null ) {
					// If not on either list (closed was checked prior) and is a walkable location, create node and add to open list
					AStarAdHocPathfinderNode tmpNode = new AStarAdHocPathfinderNode();
					tmpNode.originCost = nodeOriginCost;
					tmpNode.destinCost = nodeDestinCost;
					tmpNode.UpdateWeight();
					tmpNode.parent = currentNode;
					tmpNode.position = nodeVectorCorrected;
					tmpNode.hashCode = nodeHashCode;
					openList.Add( tmpNode );
				}
			}
		}
		
		// Show debug objects
		if( debugMode ) {				
			ShowDebugObjects(currentNode);
		}
		
		// Path search time compilation
		float endPathSearchTime = Time.realtimeSinceStartup; // DEBUG ONLY!!
		estimatedSearchTime += (endPathSearchTime-beginPathSearchTime);
		
		// If the loop terminates with members still on the open list, the path has been found
		if( ((BinaryHeap)openList).Count() > 0 ) {
			Debug.Log("Path found!");
			Debug.Log("Search Time (Internal): " + estimatedSearchTime);
			UpdateDebugText("Path Found! Search Time: " + estimatedSearchTime);
			FoundPath( currentNode );
		} else {
			// Otherwise, the pathfinder ran out of open list items and the path was not possible.
			// This only occurs when the pathfinder has reached every accessible location and the target 
			// was not found (usually small maps) - likely due to barriers.
			UpdateDebugText("No Path Found");
			Debug.Log("No Path Found");
			DidNotFindPath();
		}
	}
	
	// This collects all the node objects into an ordered vector3 ArrayList
	public ArrayList PostProcessNodeList( AStarAdHocPathfinderNode lastNode ) {
		// Array to store all node positions
		ArrayList allPositions = new ArrayList();
	
		// Traverse up the nodes to get the path
		AStarAdHocPathfinderNode currentNode = lastNode;
		while( currentNode.parent != null ) {
			allPositions.Insert(0,currentNode.position);
			currentNode = currentNode.parent;
		}
				
		// If distance between allPositions[count-2] -> target < allPositions[count-2] -> allPositions[count-1] + allPositions[count-1] -> target, 
		// then the last position overshot the target and needs to be removed
		// This prevents overshooting from the last (closest) grid point to the target
		float distFromSecondToLastToTarget = Vector3.Distance( (Vector3)allPositions[allPositions.Count-2], target );
		float distFromSecondToLastToLastToTarget = Vector3.Distance( (Vector3)allPositions[allPositions.Count-2], (Vector3)allPositions[allPositions.Count-1] ) + Vector3.Distance( (Vector3)allPositions[allPositions.Count-1], target );
		if( distFromSecondToLastToTarget < distFromSecondToLastToLastToTarget ) {
			allPositions.RemoveAt(allPositions.Count-1);
		}
		
		// Now add the target positon
		allPositions.Add(target);
		
		return allPositions;
	}
	
	// A node's hash code is extremely important as it allows quick access via hash table
	string ConstructNodeHashCode( Vector3 position ) {
		return "X"+position.x+"Z"+position.z;
	}
	
	// Calculate the approximate distance in 3D space from one Vector3 to another
	float EstimatedCostToVector( Vector3 fromVector, Vector3 toVector ) {		
		if( useOptimalHeuristics ) {
			return Vector3.Distance( fromVector, toVector );
		} else {
			// Manhattan Cube
			return Mathf.Abs(toVector.x-fromVector.x) + Mathf.Abs(toVector.z-fromVector.z) + Mathf.Abs(toVector.y-fromVector.y);
		}
	}
	
	// Calculate the real cast from one vector to another (usually this is should only used for Node->Node analysis)
	float ActualCostToVector( Vector3 fromVector, Vector3 toVector ) {
		// This need to be VASTLY improved based on slope, objects, etc		
		return Vector3.Distance( fromVector, toVector );
	}
	
	
	bool IsWalkable( Vector3 fromPosition, Vector3 toPosition, ref float yHit ) {
		// This needs to exclude invalid locations
		// Supports entering into a position from another
		bool walkable = false;
		
		RaycastHit hit;
		toPosition += new Vector3(0,raycastHeight/2,0);
		if( Physics.Raycast(toPosition, -Vector3.up, out hit, raycastHeight) ) {
			// Redefine toPosition
			toPosition = hit.point;
						
			// Slope Check
			float deltaPositionAngle = Mathf.Atan(Mathf.Abs(hit.point.y - fromPosition.y) / Mathf.Abs(hit.point.magnitude - fromPosition.magnitude)) * Mathf.Rad2Deg;
			float terrainSlope = Vector3.Angle(hit.normal, Vector3.up);
			
			if( deltaPositionAngle <= maxAngle && terrainSlope <= maxAngle ) {
				yHit = hit.point.y;
				walkable = true;
			}
			
			// Tag check
			if( walkable ) {
				if( OOBTags[hit.transform.tag] != null ) {
					walkable = false;
				}
			}
		}
		return walkable;
	}
	
	bool IsWalkable( Vector3 fromPosition, Vector3 toPosition ) {
		// This needs to exclude invalid locations
		// Supports entering into a position from another
		float yHit = -Mathf.Infinity;
		return IsWalkable(fromPosition, toPosition, ref yHit);
	}
	
	bool IsWalkable( Vector3 position ) {
		// This needs to exclude invalid locations
		// Supports entering into a position from another
		bool walkable = false;
		
		RaycastHit hit;
		position += new Vector3(0,raycastHeight/2,0);
		if( Physics.Raycast(position, -Vector3.up, out hit, raycastHeight) ) {
			// Redefine toPosition
			position = hit.point;
			
			// Slope Check
			float terrainSlope = Vector3.Angle(hit.normal, Vector3.up);
			
			if( terrainSlope <= maxAngle ) {
				walkable = true;
			}
			
			// Tag check
			if( walkable ) {
				if( OOBTags[hit.transform.tag] != null ) {
					walkable = false;
				}
			}
		}
		return walkable;
	}
	
	void RemoveAllDebugObjects( ) {
		foreach( GameObject debugObjectInstance in debugObjects ) {
			GameObject.Destroy( debugObjectInstance );
		}
	}
	
	void ShowDebugObjects( AStarAdHocPathfinderNode _lastNode ) {
		AStarAdHocPathfinderNode lastNode = _lastNode;
				
		if( debugPathObject != null ) {
			do {
				GameObject debugObject = Instantiate( debugPathObject, lastNode.position, Quaternion.identity ) as GameObject;
				debugObjects.Add (debugObject);
				lastNode = lastNode.parent;
			} while( lastNode != null );
		}
		
		if( debugOpenListObject != null ) {
			IDictionaryEnumerator enumerator = openList.GetEnumerator();
			while( enumerator.MoveNext() ) {
				// Perform logic on the item
				GameObject debugObject = Instantiate( debugOpenListObject, ((AStarAdHocPathfinderNode)enumerator.Current).position, Quaternion.identity ) as GameObject;
				debugObjects.Add (debugObject);
			}
		}
		
		if( debugClosedListObject != null ) {
			ICollection allClosedListNodes = closedList.Values;
			IEnumerator enumerator = allClosedListNodes.GetEnumerator();
			while( enumerator.MoveNext() ) {
				// Perform logic on the item
				GameObject debugObject = Instantiate( debugClosedListObject, ((AStarAdHocPathfinderNode)enumerator.Current).position, Quaternion.identity ) as GameObject;
				debugObjects.Add (debugObject);
			}
		}
	}
	
	private void UpdateDebugText( string message ) {
		if( debugTextStatus != null && debugMode ) {
			debugTextStatus.text = message;
		}
	}
	
	public ArrayList SmoothPath( ArrayList originalList, int passes = 1 ) {
		ArrayList newList = originalList;
		for( int currentPass = 0; currentPass < passes; currentPass++ ) {
			// Variable to keep list count
			int currListCount = newList.Count;
			for( int i = 0; i < currListCount; i++ ) {
				// Make sure it has points to smooth
				if( currListCount > (i+2) ) {
					float hypotDist = Vector3.Distance( (Vector3)newList[i], (Vector3)newList[i+2] );
					float squareDist = Vector3.Distance( (Vector3)newList[i], (Vector3)newList[i+1] ) + Vector3.Distance( (Vector3)newList[i+1], (Vector3)newList[i+2] );
					// Determine if the hypot distance is shorter than the combined dist
					// This will determine if the points are in a straight line (can't be smoothed) or angle (can be smoothed)
					if( hypotDist < squareDist ) {
						// Now determine if can walk the hypot path
						float yHit = -Mathf.Infinity;
						if( IsWalkable( (Vector3)newList[i], (Vector3)newList[i+2], ref yHit ) ) {
							// If is walkable, then smooth path by removing the square point
							newList.RemoveAt(i+1);
							// Update count
							currListCount = newList.Count;
						}
					}
				}
			}
		}
		return newList;
	}
}

public class AStarAdHocPathfinderNode {
	
	public float originCost;
	public float destinCost;
	public float weight;
	public AStarAdHocPathfinderNode parent;
	public Vector3 position;
	public string hashCode;
	
	public void UpdateWeight() {
		weight = originCost + destinCost;
	}
	
	// An optimization may be to hold reference to the surrounding 8 
	// nodes (TL, TC, TR, ML, MC, MR, BL, BC, BR) but that may come later

}

// Heavily modified, but thanks to David Cumps for this code/explaination/inspiration: http://weblogs.asp.net/cumpsd/371719
public class BinaryHeap {
	private Hashtable binaryHeap;
	private int numberOfItems; // Count including blank
	private Hashtable binaryHeapHashCodeIndex; // Key: HashCode, Value: Index
	
	public BinaryHeap( ) {
		binaryHeap = new Hashtable( );
		binaryHeapHashCodeIndex = new Hashtable( );
		numberOfItems = 1;
	}
	
	public void Add( AStarAdHocPathfinderNode node ) {
		
		// Add node to heap
		this.binaryHeap[this.numberOfItems] = node;
		
		// Set node's hashcode to index
		binaryHeapHashCodeIndex[node.hashCode] = this.numberOfItems;
		
		Int32 bubbleIndex = this.numberOfItems;
		while( bubbleIndex != 1 ) {
			Int32 parentIndex = bubbleIndex / 2;
			if( ((AStarAdHocPathfinderNode)this.binaryHeap[bubbleIndex]).weight <= ((AStarAdHocPathfinderNode)this.binaryHeap[parentIndex]).weight ) {
				
				// Update Node Position in Heap
				AStarAdHocPathfinderNode tmpNode = this.binaryHeap[parentIndex] as AStarAdHocPathfinderNode;
				this.binaryHeap[parentIndex] = this.binaryHeap[bubbleIndex];
				this.binaryHeap[bubbleIndex] = tmpNode;
				
				// Update both Node's hashcode index
				binaryHeapHashCodeIndex[((AStarAdHocPathfinderNode)this.binaryHeap[parentIndex]).hashCode] = parentIndex;
				binaryHeapHashCodeIndex[((AStarAdHocPathfinderNode)this.binaryHeap[bubbleIndex]).hashCode] = bubbleIndex;
				
				bubbleIndex = parentIndex;
			} else {
				break;
			}
		}             
		this.numberOfItems++;
	}
	
	public AStarAdHocPathfinderNode Remove( ) {
		
		this.numberOfItems--;
		AStarAdHocPathfinderNode returnItem = this.binaryHeap[1] as AStarAdHocPathfinderNode;
		
		// Remove first node from hashCodeIndex
		binaryHeapHashCodeIndex.Remove( returnItem.hashCode );
		
		// DOn't continue if there's no nodes left
		if( this.numberOfItems <= 1 ) {
			return returnItem;
		}
				
		// Bring last node to first
		this.binaryHeap[1] = this.binaryHeap[this.numberOfItems];
		
		// Remove last node
		this.binaryHeap.Remove( this.numberOfItems );
		
		// Update first(formally last) node hashCodeIndex
		binaryHeapHashCodeIndex[((AStarAdHocPathfinderNode)this.binaryHeap[1]).hashCode] = 1;
		
		Int32 swapItem = 1, parent = 1;
		do {
			parent = swapItem;
			if( (2 * parent + 1) < this.numberOfItems ) {
				// Both children exist
				if( ((AStarAdHocPathfinderNode)this.binaryHeap[parent]).weight >= ((AStarAdHocPathfinderNode)this.binaryHeap[2 * parent]).weight ) {
					swapItem = 2 * parent;
				}
				if( ((AStarAdHocPathfinderNode)this.binaryHeap[swapItem]).weight >= ((AStarAdHocPathfinderNode)this.binaryHeap[2 * parent + 1]).weight ) {
					swapItem = 2 * parent + 1;
				}
			} else if( ( 2 * parent ) < this.numberOfItems ) {
				// Only one child exists
				if( ((AStarAdHocPathfinderNode)this.binaryHeap[parent]).weight >= ((AStarAdHocPathfinderNode)this.binaryHeap[2 * parent]).weight ) {
					swapItem = 2 * parent;
				}
			}
			// One if the parent's children are smaller or equal, swap them
			if( parent != swapItem ) {
				AStarAdHocPathfinderNode tmpNode = this.binaryHeap[parent] as AStarAdHocPathfinderNode;
				this.binaryHeap[parent] = this.binaryHeap[swapItem];
				this.binaryHeap[swapItem] = tmpNode;
				
				// Update both Node's hashcode index
				binaryHeapHashCodeIndex[((AStarAdHocPathfinderNode)this.binaryHeap[parent]).hashCode] = parent;
				binaryHeapHashCodeIndex[((AStarAdHocPathfinderNode)this.binaryHeap[swapItem]).hashCode] = swapItem;
			}
		} while( parent != swapItem );
		
		return returnItem;
	}
	
	public void Update( AStarAdHocPathfinderNode node ) {
	
		int bubbleIndex = IndexOfNodeWithHashCode( node.hashCode );
		
		while( bubbleIndex != 1 ) {
			Int32 parentIndex = bubbleIndex / 2;
			if( ((AStarAdHocPathfinderNode)this.binaryHeap[bubbleIndex]).weight <= ((AStarAdHocPathfinderNode)this.binaryHeap[parentIndex]).weight ) {
				AStarAdHocPathfinderNode tmpNode = this.binaryHeap[parentIndex] as AStarAdHocPathfinderNode;
				this.binaryHeap[parentIndex] = this.binaryHeap[bubbleIndex];
				this.binaryHeap[bubbleIndex] = tmpNode;
				
				// Update both Node's hashcode index
				binaryHeapHashCodeIndex[((AStarAdHocPathfinderNode)this.binaryHeap[parentIndex]).hashCode] = parentIndex;
				binaryHeapHashCodeIndex[((AStarAdHocPathfinderNode)this.binaryHeap[bubbleIndex]).hashCode] = bubbleIndex;
				
				bubbleIndex = parentIndex;
			} else {
				break;
			}
		}
	}
	
	public int IndexOfNodeWithHashCode( string nodeHashCode ) {
		int? index = (int?)binaryHeapHashCodeIndex[nodeHashCode];	
		if( index != null ) {
			return (int)index;
		}
		return -1;
	}
	
	public AStarAdHocPathfinderNode NodeWithHashCode( string nodeHashCode ) {
		int index = IndexOfNodeWithHashCode( nodeHashCode );
		if( index != -1 ) {
			return (AStarAdHocPathfinderNode)binaryHeap[index];
		} else {
			return null;
		}
	}
	
	public int Count( ) {
		return this.numberOfItems;
	}
	
	public virtual IDictionaryEnumerator GetEnumerator( ) {
		return (IDictionaryEnumerator)binaryHeap.Values.GetEnumerator();
	}

}
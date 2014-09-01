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
using System.Collections;

[RequireComponent( typeof( AStarAdHocPathfinder ) )]

public class AStarAdHocSimpleMovement : MonoBehaviour {
	
	public float speed = 1.0f;
	public bool isMoving = false;
	
	private AStarAdHocPathfinder pathfinder;
	private ArrayList currentPathPositions;
	
	private bool shouldMove = false;
	
	// Debug Mode ONLY
	public bool debugMode = false;
	public GameObject targetGO;
	//
	
	void Start () {
		pathfinder = transform.GetComponent<AStarAdHocPathfinder>();
		if( pathfinder == null ) {
			Debug.LogWarning("Didn't find pathfinder component for object: " + gameObject);
		}
		
		if( debugMode ) {
			SetNewTargetPosition( targetGO.transform.position, true );
		}
	}
	
	void Update () {
		if( pathfinder.finishedPathfinding && pathfinder.firstNode != null ) {
			pathfinder.Reset();
			
			currentPathPositions = pathfinder.PostProcessNodeList( pathfinder.firstNode );
			shouldMove = true;
		}
	
		if( shouldMove ) {
			isMoving = true;
			
			if( !IsAtNextPosition() ) {
				// Move toward next position
				var step = speed * Time.deltaTime;
				transform.position = Vector3.MoveTowards(transform.position, (Vector3)currentPathPositions[0], step);
				
				// Rotate toward next position 
				Vector3 targetDir = (Vector3)currentPathPositions[0] - transform.position;
				Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDir, step/3, 0.0F);
				transform.rotation = Quaternion.LookRotation(newDir);
				transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, 0); 
			} else {
				// Removes position currently at so we can move to the next one
				currentPathPositions.RemoveAt(0);
				
				// Check to see if there are any more points, if not, finished
				if( currentPathPositions.Count == 0 ) {
					Finished();
				}
			}
		}
	}
	
	public void SetNewTargetPosition( Vector3 targetPosition, bool autoStartMovement = false ) {
		pathfinder.Reset();
	
		pathfinder.target = targetPosition;
		pathfinder.start = transform.position;
		
		pathfinder.FindPathToTarget();
	}	
	
	bool IsAtNextPosition( ) {
		if( transform.position == ((Vector3)currentPathPositions[0]) ) {
			return true;
		} else {
			return false;
		}
	}
	
	void MoveToNextPosition( ) {
		shouldMove = true;
	}
	
	public void PauseMovement( ) {
		shouldMove = false;
	}
	
	public void ContinueMovement( ) {
		shouldMove = true;
	}
	
	void Finished( ) {
		isMoving = false;
		PauseMovement();
	}
}

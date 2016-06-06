using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BoardScroller : MonoBehaviour 
{
	public Vector3 TargetPosition { get; set; }
	public Vector3 StartPosition { get; set; }
	public bool ShouldScroll { get; set; }
	
	private float scrollPercentOfRow_;
	private GameBoard board_;
	
	private void Awake()
	{
		board_ = gameObject.GetComponent<GameBoard>();
		UpdateTargetPositionForScrolling();
	}
	
	public void UpdateScroll(float scrollAmount) {
		scrollPercentOfRow_ += scrollAmount;
		// slide the game object this is attached to up.
		UpdateTransform();
		
	}

	public void UpdateTransform() {
		this.transform.localPosition = Vector3.Lerp(StartPosition, TargetPosition, 
			(scrollPercentOfRow_ - GlobalTuning.Instance.SecondsPerRowAdd) / GlobalTuning.Instance.SecondsPerRowAdd);
	}	

	public void UpdateTargetPositionForScrolling()
	{
		this.transform.localPosition = StartPosition = TargetPosition;
		var newTargetPosition = this.transform.localPosition;
		newTargetPosition.y += board_.pieceSpacing;
		TargetPosition = newTargetPosition;
	}
}

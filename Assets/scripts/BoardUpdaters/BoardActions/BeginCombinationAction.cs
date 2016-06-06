public class BeginCombinationAction : BoardAction 
{	
	private float _combineStartTime;
	public BeginCombinationAction(int row, int column, float combineStartTime) : base(row, column) {
		_combineStartTime = combineStartTime;
	}
	public override void PerformAction(GameBoard board) {
		var piece = board.GetPieceAt(Row, Column);
		piece.BeginCombo(_combineStartTime);
	}
}
public class BoardAction 
{
	public BoardAction() {}
	public BoardAction(int row, int column) {
		Row = row;
		Column = column;
	}
	public int Row;
	public int Column;
	public virtual void PerformAction(GameBoard board) {}
}
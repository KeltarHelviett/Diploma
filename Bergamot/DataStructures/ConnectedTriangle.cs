using System.Drawing;

namespace Bergamot.DataStructures
{
	// 3 vertices and 3 adjacent triangles
	public class ConnectedTriangle
	{
		public Point?[] Vertices = new Point?[3];
		public ConnectedTriangle[] Triangles = new ConnectedTriangle[3];
	}
}

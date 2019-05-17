using System.Collections.Generic;
using System.Drawing;

namespace Bergamot.DataStructures
{
	public class Polygon
	{
		public PolygonEdge Start { get; set; }
		public PolygonEdge End { get; set; }

		public bool InsertEdge(PolygonEdge edge)
		{
			if (Start == null) {
				Start = edge;
				End = edge;
				return true;
			}
			if (End.V2 == edge.V1) {
				End.Next = edge;
				End = edge;
				return true;
			}
			return false;
		}

		public bool TryClose()
		{
			if (End.V2 == Start.V1) {
				End.Next = Start;
				return true;
			}
			return false;
		}

		public List<ConnectedTriangle> Triangulate(PointF point)
		{
			return Start.Triangulate(point);
		}
	}
}

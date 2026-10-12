using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldGeneration.Models
{
	public class Line
	{
		protected Vector2 p1 = Vector2.zero;

		protected Vector2 p2 = Vector2.zero;

		protected bool isVertical;

		protected bool isHorizontal;

		protected float length;

		protected float? gradiant;

		public Vector2 P1
		{
			get
			{
				return p1;
			}
			set
			{
				p1 = value;
				Recalculate();
			}
		}

		public Vector2 P2
		{
			get
			{
				return p2;
			}
			set
			{
				p2 = value;
				Recalculate();
			}
		}

		public Vector2 Direction => new Vector2(p2.x - p1.x, p2.y - p1.y);

		public bool IsVertical => isVertical;

		public bool IsHorizontal => isHorizontal;

		public bool IsZerolLength
		{
			get
			{
				if (p1 == p2)
				{
					return true;
				}
				return false;
			}
		}

		public float? YIntersect
		{
			get
			{
				if (gradiant.HasValue)
				{
					return p1.y - Gradiant.Value * p1.x;
				}
				return null;
			}
		}

		public float? Gradiant => gradiant;

		public float Length => length;

		public static Vector2? Intersection(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
		{
			float? num = IntersectionDist(p1, p2, q1, q2);
			if (num.HasValue)
			{
				return new Vector2(p1.x + num.Value * (p2.x - p1.x), p1.y + num.Value * (p2.y - p1.y));
			}
			return null;
		}

		public static float? IntersectionDist(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
		{
			if (p1 == p2 || q1 == q2)
			{
				return null;
			}
			float num = (q2.y - q1.y) * (p2.x - p1.x) - (q2.x - q1.x) * (p2.y - p1.y);
			float num2 = ((q2.x - q1.x) * (p1.y - q1.y) - (q2.y - q1.y) * (p1.x - q1.x)) / num;
			float num3 = ((p2.x - p1.x) * (p1.y - q1.y) - (p2.y - p1.y) * (p1.x - q1.x)) / num;
			if (num2 > 0f && num2 < 1f && num3 > 0f && num3 < 1f)
			{
				return num2;
			}
			return null;
		}

		public static Vector2? Intersection(Line line1, Line line2, bool line1Infinite, bool line2Infinite)
		{
			if (!line1.IsZerolLength && !line2.IsZerolLength)
			{
				float? num = line1.Gradiant;
				float? num2 = line2.Gradiant;
				float? num3 = null;
				float? num4 = null;
				if (num != num2 || (line1.isHorizontal && line2.isVertical) || (line1.isVertical && line1.isHorizontal))
				{
					if (line1.isVertical)
					{
						num3 = line1.p1.x;
					}
					else if (line2.isVertical)
					{
						num3 = line2.p1.x;
					}
					if (line1.isHorizontal)
					{
						num4 = line1.p1.y;
					}
					else if (line2.isHorizontal)
					{
						num4 = line2.p1.y;
					}
					if (!num3.HasValue)
					{
						float num5 = num.Value - num2.Value;
						num3 = (line2.YIntersect.Value - line1.YIntersect.Value) / num5;
					}
					if (!num4.HasValue)
					{
						float num6 = 0f;
						float num7 = 0f;
						if (num.HasValue)
						{
							num6 = num.Value * num3.Value;
							num7 = line1.YIntersect.Value;
						}
						else
						{
							num6 = num2.Value * num3.Value;
							num7 = line2.YIntersect.Value;
						}
						num4 = num6 + num7;
					}
					Vector2 vector = new Vector2(num3.Value, num4.Value);
					if ((line1Infinite || line1.ToRectangle().Contains(vector)) && (line2Infinite || line2.ToRectangle().Contains(vector)))
					{
						return vector;
					}
				}
			}
			return null;
		}

		public PrecisionRectangle ToRectangle()
		{
			return new PrecisionRectangle(p1, p2);
		}

		protected void Recalculate()
		{
			isHorizontal = p1.y == p2.y;
			isVertical = p1.x == p2.x;
			if (isVertical)
			{
				gradiant = null;
			}
			else
			{
				gradiant = (p1.y - p2.y) / (p1.x - p2.x);
			}
			length = Vector2.Distance(p1, p2);
		}

		public Line()
		{
		}

		public Line(Vector2 point1, Vector2 point2)
		{
			P1 = point1;
			P2 = point2;
		}
	}
}

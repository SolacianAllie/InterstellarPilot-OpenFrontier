using System;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldGeneration.Models
{
	public class PrecisionRectangle
	{
		public float X;

		public float Y;

		protected float width;

		protected float height;

		public float Width
		{
			get
			{
				return width;
			}
			set
			{
				width = value;
				if (width < 0f)
				{
					throw new ArgumentException();
				}
			}
		}

		public float Height
		{
			get
			{
				return height;
			}
			set
			{
				height = value;
				if (height < 0f)
				{
					throw new ArgumentException();
				}
			}
		}

		public float Top
		{
			get
			{
				return Y;
			}
			set
			{
				Y = value;
			}
		}

		public float Bottom
		{
			get
			{
				return Y - height;
			}
			set
			{
				Y = value + height;
			}
		}

		public float Left
		{
			get
			{
				return X;
			}
			set
			{
				X = value;
			}
		}

		public float Right
		{
			get
			{
				return X + width;
			}
			set
			{
				X = value - width;
			}
		}

		public Vector2 Center => new Vector2(X + Width / 2f, Bottom + Height / 2f);

		public PrecisionRectangle()
		{
		}

		public PrecisionRectangle(float x, float y, float width, float height)
		{
			X = x;
			Y = y;
			if (width < 0f || height < 0f)
			{
				throw new ArgumentException("Negative parameter");
			}
			Width = width;
			Height = height;
		}

		public PrecisionRectangle(Vector2 p1, Vector2 p2)
		{
			X = Math.Min(p1.x, p2.x);
			Y = Math.Max(p1.y, p2.y);
			width = Math.Max(p1.x, p2.x) - X;
			height = Y - Math.Min(p1.y, p2.y);
		}

		public bool Intersects(PrecisionRectangle rect2)
		{
			if (((Left >= rect2.Left && Left <= rect2.Right) || (Right >= rect2.Left && Right <= rect2.Right) || (Left <= rect2.Left && Right >= rect2.Right)) && ((Top >= rect2.Bottom && Top <= rect2.Top) || (Bottom >= rect2.Bottom && Bottom <= rect2.Top) || (Top >= rect2.Top && Bottom <= rect2.Bottom)))
			{
				return true;
			}
			return false;
		}

		public bool Contains(PrecisionRectangle obj2)
		{
			if (obj2.Left >= Left && obj2.Right <= Right && obj2.Bottom >= Bottom && obj2.Top <= Top)
			{
				return true;
			}
			return false;
		}

		public bool Contains(Vector2 position)
		{
			if (position.x >= Left && position.x <= Right && position.y >= Bottom && position.y <= Top)
			{
				return true;
			}
			return false;
		}

		public PrecisionRectangle Merge(PrecisionRectangle obj2)
		{
			float num = Math.Min(Left, obj2.Left);
			float num2 = Math.Max(Right, obj2.Right) - num;
			float num3 = Math.Max(Top, obj2.Top);
			float num4 = num3 - Math.Min(Bottom, obj2.Bottom);
			return new PrecisionRectangle(num, num3, num2, num4);
		}

		public PrecisionRectangle CalcIntersectingRectangle(PrecisionRectangle rect2)
		{
			PrecisionRectangle precisionRectangle = new PrecisionRectangle();
			if (Intersects(rect2))
			{
				if (Left >= rect2.Left && Left <= rect2.Right)
				{
					precisionRectangle.X = Left;
				}
				else
				{
					precisionRectangle.X = rect2.Left;
				}
				if (Right >= rect2.Left && Right <= rect2.Right)
				{
					precisionRectangle.Width = Right - precisionRectangle.X;
				}
				else
				{
					precisionRectangle.Width = rect2.Right - precisionRectangle.X;
				}
				if (Top <= rect2.Top && Top >= rect2.Bottom)
				{
					precisionRectangle.Y = Top;
				}
				else
				{
					precisionRectangle.Y = rect2.Top;
				}
				if (Bottom <= rect2.Top && Bottom >= rect2.Bottom)
				{
					precisionRectangle.Height = precisionRectangle.Y - Bottom;
				}
				else
				{
					precisionRectangle.Height = precisionRectangle.Y - rect2.Bottom;
				}
			}
			return precisionRectangle;
		}
	}
}

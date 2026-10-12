using System;
using UnityEngine;

namespace OpenFrontier.IP
{
	public class BitMaskAttribute : PropertyAttribute
	{
		public Type propType;

		public BitMaskAttribute(Type aType)
		{
			propType = aType;
		}
	}
}

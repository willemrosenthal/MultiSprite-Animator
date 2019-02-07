using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace MultiSprite {

/// Handy extention methods
public static class ExtentionMethods
{
	public static Color WithAlpha(this Color col, float alpha )
	{
		return new Color(col.r,col.g,col.b,alpha);
	}
}

public static class Utils
{
	/// Returns float value snapped to closest point
	public static float Snap(float value, float snapTo)
	{
		if ( snapTo <= 0 ) return value;
		return Mathf.Round(value / snapTo) * snapTo;
	}

	/// Swaps two objects
	public static void Swap<T>(ref T lhs, ref T rhs)
	{
		T temp;
		temp = lhs;
		lhs = rhs;
		rhs = temp;
	}

	// Creates new instance of passed object and copies variables
	public static T Clone<T>(T from) where T : new()
	{
		T result = new T();

		FieldInfo[] finfos = from.GetType().GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
		for ( int i = 0; i < finfos.Length; ++i )
		{			
			finfos[i].SetValue(result, finfos[i].GetValue(from));
		}
		return result;
	}

	public static class BitMask
	{			
		// And some static functions if you don't wanna construt the bitmask  and just wanna pass in/out an int
		public static int SetAt(int mask, int index) { return mask | 1 << index; }
		public static int UnsetAt(int mask, int index)  { return mask & ~(1 << index); }
		public static bool IsSet(int mask, int index) { return (mask & 1 << index) != 0; }

		public static uint GetNumberOfSetBits(uint i)
		{
			// From http://stackoverflow.com/questions/109023/how-to-count-the-number-of-set-bits-in-a-32-bit-integer
			i = i - ((i >> 1) & 0x55555555);
			i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
			return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
		}
	}

	// Returns angle normalized 2d direction vector in degrees between 0 and 360
	public static float GetDirectionAngle( this Vector2 directionNormalised )
	{		
		if ( Mathf.Approximately( directionNormalised.y, 0) )
		{
			if ( directionNormalised.x < 0 )
				return 180.0f;
			else 
				return 0;
		}
		else if ( Mathf.Approximately( directionNormalised.x, 0 ) )
		{
			if ( directionNormalised.y < 0 )
				return 270.0f;
			else 
				return 90.0f;
		}
		
		return Mathf.Repeat( Mathf.Rad2Deg * Mathf.Atan2(directionNormalised.y, directionNormalised.x), 360 );
	}
}
}
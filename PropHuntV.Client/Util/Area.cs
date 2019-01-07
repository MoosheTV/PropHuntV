using System;
using System.Linq;
using CitizenFX.Core;

namespace PropHuntV.Client.Util
{
	public class Area
	{
		public readonly float[][] Polygon;
		public readonly float MinZ;
		public readonly float MaxZ;

		public Area( float[][] polygon, float minZ, float maxZ ) {
			Polygon = polygon;
			MinZ = minZ;
			MaxZ = maxZ;
		}

		/// <summary>
		/// Determines if the provided vector is within this Area.
		/// </summary>
		/// <param name="vector">The vector to check against the polygon.</param>
		/// <returns></returns>
		public bool Contains( Vector3 vector ) {
			if( vector.Z < MinZ || vector.Z > MaxZ ) {
				return false;
			}
			int i, j;
			var count = Polygon.Length;
			var intersects = false;
			for( i = 0, j = count - 1; i < count; j = i++ ) {
				if( Polygon[i][1] > vector.Y != Polygon[j][1] > vector.Y &&
					vector.X < (Polygon[j][0] - Polygon[i][0]) * (vector.Y - Polygon[i][1]) / (Polygon[j][1] - Polygon[i][1]) + Polygon[i][0] )
					intersects = !intersects;
			}
			return intersects;
		}

		/// <summary>
		/// Returns the 2D area of the polygon in squared meters.
		/// </summary>
		/// <returns></returns>
		public float Area2DSquared() {
			var area = 0f;
			for( var i = 0; i < Polygon.Length; i++ ) {
				var vector = Polygon[i];
				var nextVector = Polygon[i + 1 >= Polygon.Length ? 0 : i + 1];
				area += (nextVector[0] - Polygon[i][0]) * (nextVector[1] + vector[1]) / 2;
			}
			return Math.Abs( area );
		}
	}
}

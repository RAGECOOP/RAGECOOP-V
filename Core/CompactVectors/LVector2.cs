using System;
using System.Collections.Generic;
using System.Globalization;
using GTA.Math;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RageCoop.Core
{
    internal struct LVector2 : IEquatable<LVector2>
    {
        /// <summary>
        /// Gets or sets the X component of the vector.
        /// </summary>
        /// <value>The X component of the vector.</value>
        public float X;

        /// <summary>
        /// Gets or sets the Y component of the vector.
        /// </summary>
        /// <value>The Y component of the vector.</value>
        public float Y;

        /// <summary>
        /// Initializes a new instance of the <see cref="LVector2"/> class.
        /// </summary>
        /// <param name="x">Initial value for the X component of the vector.</param>
        /// <param name="y">Initial value for the Y component of the vector.</param>
        public LVector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Returns this vector with a magnitude of 1.
        /// </summary>
        public LVector2 Normalized => Normalize(new LVector2(X, Y));

        /// <summary>
        /// Returns a null vector. (0,0)
        /// </summary>
        public static LVector2 Zero => new LVector2(0.0f, 0.0f);

        /// <summary>
        /// The X unit <see cref="LVector2"/> (1, 0).
        /// </summary>
        public static LVector2 UnitX => new LVector2(1.0f, 0.0f);

        /// <summary>
        /// The Y unit <see cref="LVector2"/> (0, 1).
        /// </summary>
        public static LVector2 UnitY => new LVector2(0.0f, 1.0f);

        /// <summary>
        /// Returns the up vector. (0,1)
        /// </summary>
        public static LVector2 Up => new LVector2(0.0f, 1.0f);

        /// <summary>
        /// Returns the down vector. (0,-1)
        /// </summary>
        public static LVector2 Down => new LVector2(0.0f, -1.0f);

        /// <summary>
        /// Returns the right vector. (1,0)
        /// </summary>
        public static LVector2 Right => new LVector2(1.0f, 0.0f);

        /// <summary>
        /// Returns the left vector. (-1,0)
        /// </summary>
        public static LVector2 Left => new LVector2(-1.0f, 0.0f);

        /// <summary>
        /// Gets or sets the component at the specified index.
        /// </summary>
        /// <value>The value of the X or Y component, depending on the index.</value>
        /// <param name="index">The index of the component to access. Use 0 for the X component and 1 for the Y component.</param>
        /// <returns>The value of the component at the specified index.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the <paramref name="index"/> is out of the range [0, 1].</exception>
        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return X;
                    case 1:
                        return Y;
                }

                throw new ArgumentOutOfRangeException("index", "Indices for Vector2 run from 0 to 1, inclusive.");
            }

            set
            {
                switch (index)
                {
                    case 0:
                        X = value;
                        break;
                    case 1:
                        Y = value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("index", "Indices for Vector2 run from 0 to 1, inclusive.");
                }
            }
        }

        /// <summary>
        /// Calculates the length of the vector.
        /// </summary>
        /// <returns>The length of the vector.</returns>
        public float Length()
        {
            return (float)System.Math.Sqrt((X * X) + (Y * Y));
        }

        /// <summary>
        /// Calculates the squared length of the vector.
        /// </summary>
        /// <returns>The squared length of the vector.</returns>
        public float LengthSquared()
        {
            return (X * X) + (Y * Y);
        }

        /// <summary>
        /// Converts the vector into a unit vector.
        /// </summary>
        public void Normalize()
        {
            float length = Length();
            if (length == 0)
            {
                return;
            }

            float num = 1 / length;
            X *= num;
            Y *= num;
        }

        /// <summary>
        /// Calculates the distance between two vectors.
        /// </summary>
        /// <param name="position">The second vector to calculate the distance to.</param>
        /// <returns>The distance to the other vector.</returns>
        public float DistanceTo(LVector2 position)
        {
            return (position - this).Length();
        }

        /// <summary>
        /// Calculates the squared distance between two vectors.
        /// </summary>
        /// <param name="position">The second vector to calculate the squared distance to.</param>
        /// <returns>The squared distance to the other vector.</returns>
        public float DistanceToSquared(LVector2 position)
        {
            return DistanceSquared(position, this);
        }

        /// <summary>
        /// Calculates the distance between two vectors.
        /// </summary>
        /// <param name="position1">The first vector to calculate the distance to the second vector.</param>
        /// <param name="position2">The second vector to calculate the distance to the first vector.</param>
        /// <returns>The distance between the two vectors.</returns>
        public static float Distance(LVector2 position1, LVector2 position2)
        {
            return (position1 - position2).Length();
        }

        /// <summary>
        /// Calculates the squared distance between two vectors.
        /// </summary>
        /// <param name="position1">The first vector to calculate the squared distance to the second vector.</param>
        /// <param name="position2">The second vector to calculate the squared distance to the first vector.</param>
        /// <returns>The squared distance between the two vectors.</returns>
        public static float DistanceSquared(LVector2 position1, LVector2 position2)
        {
            return (position1 - position2).LengthSquared();
        }

        /// <summary>
        /// Returns the angle in degrees between from and to.
        /// The angle returned is always the acute angle between the two vectors.
        /// </summary>
        public static float Angle(LVector2 from, LVector2 to)
        {
            return System.Math.Abs(SignedAngle(from, to));
        }

        /// <summary>
        /// Returns the signed angle in degrees between from and to.
        /// </summary>
        public static float SignedAngle(LVector2 from, LVector2 to)
        {
            return (float)((System.Math.Atan2(to.Y, to.X) - System.Math.Atan2(from.Y, from.X)) * (180.0 / System.Math.PI));
        }

        /// <summary>
        /// Converts a vector to a heading.
        /// </summary>
        public float ToHeading()
        {
            return (float)((System.Math.Atan2(X, -Y) + System.Math.PI) * (180.0 / System.Math.PI));
        }

        /// <summary>
        /// Returns a new normalized vector with random X and Y components.
        /// </summary>
        public static LVector2 RandomXY()
        {
            LVector2 v;
            double radian = CoreUtils.SafeRandom.NextDouble() * 2 * System.Math.PI;
            v.X = (float)(System.Math.Cos(radian));
            v.Y = (float)(System.Math.Sin(radian));
            v.Normalize();
            return v;
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="left">The first vector to add.</param>
        /// <param name="right">The second vector to add.</param>
        /// <returns>The sum of the two vectors.</returns>
        public static LVector2 Add(LVector2 left, LVector2 right) => new LVector2(left.X + right.X, left.Y + right.Y);

        /// <summary>
        /// Subtracts two vectors.
        /// </summary>
        /// <param name="left">The first vector to subtract.</param>
        /// <param name="right">The second vector to subtract.</param>
        /// <returns>The difference of the two vectors.</returns>
        public static LVector2 Subtract(LVector2 left, LVector2 right) => new LVector2(left.X - right.X, left.Y - right.Y);

        /// <summary>
        /// Scales a vector by the given value.
        /// </summary>
        /// <param name="value">The vector to scale.</param>
        /// <param name="scale">The amount by which to scale the vector.</param>
        /// <returns>The scaled vector.</returns>
        public static LVector2 Multiply(LVector2 value, float scale) => new LVector2(value.X * scale, value.Y * scale);

        /// <summary>
        /// Multiplies a vector with another by performing component-wise multiplication.
        /// </summary>
        /// <param name="left">The first vector to multiply.</param>
        /// <param name="right">The second vector to multiply.</param>
        /// <returns>The multiplied vector.</returns>
        public static LVector2 Multiply(LVector2 left, LVector2 right) => new LVector2(left.X * right.X, left.Y * right.Y);

        /// <summary>
        /// Scales a vector by the given value.
        /// </summary>
        /// <param name="value">The vector to scale.</param>
        /// <param name="scale">The amount by which to scale the vector.</param>
        /// <returns>The scaled vector.</returns>
        public static LVector2 Divide(LVector2 value, float scale) => new LVector2(value.X / scale, value.Y / scale);

        /// <summary>
        /// Reverses the direction of a given vector.
        /// </summary>
        /// <param name="value">The vector to negate.</param>
        /// <returns>A vector facing in the opposite direction.</returns>
        public static LVector2 Negate(LVector2 value) => new LVector2(-value.X, -value.Y);

        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>The clamped value.</returns>
        public static LVector2 Clamp(LVector2 value, LVector2 min, LVector2 max)
        {
            float x = value.X;
            x = (x > max.X) ? max.X : x;
            x = (x < min.X) ? min.X : x;

            float y = value.Y;
            y = (y > max.Y) ? max.Y : y;
            y = (y < min.Y) ? min.Y : y;

            return new LVector2(x, y);
        }

        /// <summary>
        /// Performs a linear interpolation between two vectors.
        /// </summary>
        /// <param name="start">Start vector.</param>
        /// <param name="end">End vector.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <returns>The linear interpolation of the two vectors.</returns>
        /// <remarks>
        /// This method performs the linear interpolation based on the following formula.
        /// <code>start + (end - start) * amount</code>
        /// Passing <paramref name="amount"/> a value of 0 will cause <paramref name="start"/> to be returned; a value of 1 will cause <paramref name="end"/> to be returned.
        /// </remarks>
        public static LVector2 Lerp(LVector2 start, LVector2 end, float amount)
        {
            LVector2 vector;

            vector.X = start.X + ((end.X - start.X) * amount);
            vector.Y = start.Y + ((end.Y - start.Y) * amount);

            return vector;
        }

        /// <summary>
        /// Converts the vector into a unit vector.
        /// </summary>
        /// <param name="vector">The vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
        public static LVector2 Normalize(LVector2 vector)
        {
            vector.Normalize();
            return vector;
        }

        /// <summary>
        /// Calculates the dot product of two vectors.
        /// </summary>
        /// <param name="left">First source vector.</param>
        /// <param name="right">Second source vector.</param>
        /// <returns>The dot product of the two vectors.</returns>
        public static float Dot(LVector2 left, LVector2 right) => (left.X * right.X + left.Y * right.Y);

        /// <summary>
        /// Returns the reflection of a vector off a surface that has the specified normal.
        /// </summary>
        /// <param name="vector">The source vector.</param>
        /// <param name="normal">Normal of the surface.</param>
        /// <returns>The reflected vector.</returns>
        /// <remarks>Reflect only gives the direction of a reflection off a surface, it does not determine
        /// whether the original vector was close enough to the surface to hit it.</remarks>
        public static LVector2 Reflect(LVector2 vector, LVector2 normal)
        {
            LVector2 result;
            float dot = ((vector.X * normal.X) + (vector.Y * normal.Y));

            result.X = vector.X - ((2.0f * dot) * normal.X);
            result.Y = vector.Y - ((2.0f * dot) * normal.Y);

            return result;
        }

        /// <summary>
        /// Returns a vector containing the smallest components of the specified vectors.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>A vector containing the smallest components of the source vectors.</returns>
        public static LVector2 Minimize(LVector2 left, LVector2 right)
        {
            LVector2 vector;
            vector.X = (left.X < right.X) ? left.X : right.X;
            vector.Y = (left.Y < right.Y) ? left.Y : right.Y;
            return vector;
        }
        /// <summary>
        /// Returns a vector containing the largest components of the specified vectors.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>A vector containing the largest components of the source vectors.</returns>
        public static LVector2 Maximize(LVector2 left, LVector2 right)
        {
            LVector2 vector;
            vector.X = (left.X > right.X) ? left.X : right.X;
            vector.Y = (left.Y > right.Y) ? left.Y : right.Y;
            return vector;
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="left">The first vector to add.</param>
        /// <param name="right">The second vector to add.</param>
        /// <returns>The sum of the two vectors.</returns>
        public static LVector2 operator +(LVector2 left, LVector2 right) => new LVector2(left.X + right.X, left.Y + right.Y);

        /// <summary>
        /// Subtracts two vectors.
        /// </summary>
        /// <param name="left">The first vector to subtract.</param>
        /// <param name="right">The second vector to subtract.</param>
        /// <returns>The difference of the two vectors.</returns>
        public static LVector2 operator -(LVector2 left, LVector2 right) => new LVector2(left.X - right.X, left.Y - right.Y);

        /// <summary>
        /// Reverses the direction of a given vector.
        /// </summary>
        /// <param name="value">The vector to negate.</param>
        /// <returns>A vector facing in the opposite direction.</returns>
        public static LVector2 operator -(LVector2 value) => new LVector2(-value.X, -value.Y);

        /// <summary>
        /// Scales a vector by the given value.
        /// </summary>
        /// <param name="vector">The vector to scale.</param>
        /// <param name="scale">The amount by which to scale the vector.</param>
        /// <returns>The scaled vector.</returns>
        public static LVector2 operator *(LVector2 vector, float scale) => new LVector2(vector.X * scale, vector.Y * scale);

        /// <summary>
        /// Scales a vector by the given value.
        /// </summary>
        /// <param name="vector">The vector to scale.</param>
        /// <param name="scale">The amount by which to scale the vector.</param>
        /// <returns>The scaled vector.</returns>
        public static LVector2 operator *(float scale, LVector2 vector) => new LVector2(vector.X * scale, vector.Y * scale);

        /// <summary>
        /// Scales a vector by the given value.
        /// </summary>
        /// <param name="vector">The vector to scale.</param>
        /// <param name="scale">The amount by which to scale the vector.</param>
        /// <returns>The scaled vector.</returns>
        public static LVector2 operator /(LVector2 vector, float scale) => new LVector2(vector.X / scale, vector.Y / scale);

        /// <summary>
        /// Tests for equality between two objects.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="left"/> has the same value as <paramref name="right"/>; otherwise, <see langword="false" />.</returns>
        public static bool operator ==(LVector2 left, LVector2 right) => Equals(left, right);

        /// <summary>
        /// Tests for inequality between two objects.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="left"/> has a different value than <paramref name="right"/>; otherwise, <see langword="false" />.</returns>
        public static bool operator !=(LVector2 left, LVector2 right) => !Equals(left, right);

        /// <summary>
        /// Converts a Vector2 to a Vector3 implicitly.
        /// </summary>
        public static implicit operator LVector3(LVector2 vector) => new LVector3(vector.X, vector.Y, 0);

        /// <summary>
        /// Converts the value of the object to its equivalent string representation.
        /// </summary>
        /// <returns>The string representation of the value of this instance.</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "X:{0} Y:{1}", X, Y);
        }

        /// <summary>
        /// Converts the value of the object to its equivalent string representation.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>The string representation of the value of this instance.</returns>
        public string ToString(string format)
        {
            if (format == null)
            {
                return ToString();
            }

            return string.Format(CultureInfo.CurrentCulture, "X:{0} Y:{1}", X.ToString(format, CultureInfo.CurrentCulture), Y.ToString(format, CultureInfo.CurrentCulture));
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        /// <summary>
        /// Returns a value that indicates whether the current instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">Object to make the comparison with.</param>
        /// <returns><see langword="true" /> if the current instance is equal to the specified object; otherwise, <see langword="false" />.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((LVector2)obj);
        }

        /// <summary>
        /// Returns a value that indicates whether the current instance is equal to the specified object.
        /// </summary>
        /// <param name="other">Object to make the comparison with.</param>
        /// <returns><see langword="true" /> if the current instance is equal to the specified object; <see langword="false" /> otherwise.</returns>
        public bool Equals(LVector2 other) => (X == other.X && Y == other.Y);


        public static implicit operator LVector2(Vector2 v) => new(v.X, v.Y);
        public static implicit operator Vector2(LVector2 v) => new(v.X, v.Y);
    }
}

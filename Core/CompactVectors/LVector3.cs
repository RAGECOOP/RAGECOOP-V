﻿//
// Copyright (C) 2007-2010 SlimDX Group
//
// Permission is hereby granted, free  of charge, to any person obtaining a copy of this software  and
// associated  documentation  files (the  "Software"), to deal  in the Software  without  restriction,
// including  without  limitation  the  rights  to use,  copy,  modify,  merge,  publish,  distribute,
// sublicense, and/or sell  copies of the  Software,  and to permit  persons to whom  the Software  is
// furnished to do so, subject to the following conditions:
//
// The  above  copyright  notice  and this  permission  notice shall  be included  in  all  copies  or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS",  WITHOUT WARRANTY OF  ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT  LIMITED  TO  THE  WARRANTIES  OF  MERCHANTABILITY,  FITNESS  FOR  A   PARTICULAR  PURPOSE  AND
// NONINFRINGEMENT.  IN  NO  EVENT SHALL THE  AUTHORS  OR COPYRIGHT HOLDERS  BE LIABLE FOR  ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,  OUT
// OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using GTA.Math;
using System.Globalization;
using System.Runtime.InteropServices;

namespace RageCoop.Core
{

    [Serializable]
    internal struct LVector3 : IEquatable<LVector3>
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
        /// Gets or sets the Z component of the vector.
        /// </summary>
        /// <value>The Z component of the vector.</value>
        public float Z;

        /// <summary>
        /// Initializes a new instance of the <see cref="LVector3"/> class.
        /// </summary>
        /// <param name="x">Initial value for the X component of the vector.</param>
        /// <param name="y">Initial value for the Y component of the vector.</param>
        /// <param name="z">Initial value for the Z component of the vector.</param>
        public LVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        internal LVector3(float[] values) : this(values[0], values[1], values[2])
        {
        }

        /// <summary>
        /// Returns this vector with a magnitude of 1.
        /// </summary>
        public LVector3 Normalized => Normalize(new LVector3(X, Y, Z));

        /// <summary>
        /// Returns a null vector. (0,0,0)
        /// </summary>
        public static LVector3 Zero => new(0.0f, 0.0f, 0.0f);

        /// <summary>
        /// The X unit <see cref="LVector3"/> (1, 0, 0).
        /// </summary>
        public static LVector3 UnitX => new(1.0f, 0.0f, 0.0f);

        /// <summary>
        /// The Y unit <see cref="LVector3"/> (0, 1, 0).
        /// </summary>
        public static LVector3 UnitY => new(0.0f, 1.0f, 0.0f);

        /// <summary>
        /// The Z unit <see cref="LVector3"/> (0, 0, 1).
        /// </summary>
        public static LVector3 UnitZ => new(0.0f, 0.0f, 1.0f);

        /// <summary>
        /// Returns the world Up vector. (0,0,1)
        /// </summary>
        public static LVector3 WorldUp => new(0.0f, 0.0f, 1.0f);

        /// <summary>
        /// Returns the world Down vector. (0,0,-1)
        /// </summary>
        public static LVector3 WorldDown => new(0.0f, 0.0f, -1.0f);

        /// <summary>
        /// Returns the world North vector. (0,1,0)
        /// </summary>
        public static LVector3 WorldNorth => new(0.0f, 1.0f, 0.0f);

        /// <summary>
        /// Returns the world South vector. (0,-1,0)
        /// </summary>
        public static LVector3 WorldSouth => new(0.0f, -1.0f, 0.0f);

        /// <summary>
        /// Returns the world East vector. (1,0,0)
        /// </summary>
        public static LVector3 WorldEast => new(1.0f, 0.0f, 0.0f);

        /// <summary>
        /// Returns the world West vector. (-1,0,0)
        /// </summary>
        public static LVector3 WorldWest => new(-1.0f, 0.0f, 0.0f);

        /// <summary>
        /// Returns the relative Right vector. (1,0,0)
        /// </summary>
        public static LVector3 RelativeRight => new(1.0f, 0.0f, 0.0f);

        /// <summary>
        /// Returns the relative Left vector. (-1,0,0)
        /// </summary>
        public static LVector3 RelativeLeft => new(-1.0f, 0.0f, 0.0f);

        /// <summary>
        /// Returns the relative Front vector. (0,1,0)
        /// </summary>
        public static LVector3 RelativeFront => new(0.0f, 1.0f, 0.0f);

        /// <summary>
        /// Returns the relative Back vector. (0,-1,0)
        /// </summary>
        public static LVector3 RelativeBack => new(0.0f, -1.0f, 0.0f);

        /// <summary>
        /// Returns the relative Top vector. (0,0,1)
        /// </summary>
        public static LVector3 RelativeTop => new(0.0f, 0.0f, 1.0f);

        /// <summary>
        /// Returns the relative Bottom vector as used. (0,0,-1)
        /// </summary>
        public static LVector3 RelativeBottom => new(0.0f, 0.0f, -1.0f);

        /// <summary>
        /// Gets or sets the component at the specified index.
        /// </summary>
        /// <value>The value of the X, Y or Z component, depending on the index.</value>
        /// <param name="index">The index of the component to access. Use 0 for the X component, 1 for the Y component and 2 for the Z component.</param>
        /// <returns>The value of the component at the specified index.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the <paramref name="index"/> is out of the range [0, 2].</exception>
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
                    case 2:
                        return Z;
                }

                throw new ArgumentOutOfRangeException("index", "Indices for Vector3 run from 0 to 2, inclusive.");
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
                    case 2:
                        Z = value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("index",
                            "Indices for Vector3 run from 0 to 2, inclusive.");
                }
            }
        }

        /// <summary>
        /// Calculates the length of the vector.
        /// </summary>
        /// <returns>The length of the vector.</returns>
        public float Length() => (float)(System.Math.Sqrt((X * X) + (Y * Y) + (Z * Z)));

        /// <summary>
        /// Calculates the squared length of the vector.
        /// </summary>
        /// <returns>The squared length of the vector.</returns>
        public float LengthSquared() => (X * X) + (Y * Y) + (Z * Z);

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
            Z *= num;
        }

        /// <summary>
        /// Calculates the distance between two vectors.
        /// </summary>
        /// <param name="position">The second vector to calculate the distance to.</param>
        /// <returns>The distance to the other vector.</returns>
        public float DistanceTo(LVector3 position) => (position - this).Length();

        /// <summary>
        /// Calculates the squared distance between two vectors.
        /// </summary>
        /// <param name="position">The second vector to calculate the distance to.</param>
        /// <returns>The distance to the other vector.</returns>
        public float DistanceToSquared(LVector3 position) => DistanceSquared(position, this);

        /// <summary>
        /// Calculates the distance between two vectors, ignoring the Z-component.
        /// </summary>
        /// <param name="position">The second vector to calculate the distance to.</param>
        /// <returns>The distance to the other vector.</returns>
        public float DistanceTo2D(LVector3 position)
        {
            var lhs = new LVector3(X, Y, 0.0f);
            var rhs = new LVector3(position.X, position.Y, 0.0f);

            return Distance(lhs, rhs);
        }

        /// <summary>
        /// Calculates the squared distance between two vectors, ignoring the Z-component.
        /// </summary>
        /// <param name="position">The second vector to calculate the squared distance to.</param>
        /// <returns>The distance to the other vector.</returns>
        public float DistanceToSquared2D(LVector3 position)
        {
            var lhs = new LVector3(X, Y, 0.0f);
            var rhs = new LVector3(position.X, position.Y, 0.0f);

            return DistanceSquared(lhs, rhs);
        }

        /// <summary>
        /// Calculates the distance between two vectors.
        /// </summary>
        /// <param name="position1">The first vector to calculate the distance to the second vector.</param>
        /// <param name="position2">The second vector to calculate the distance to the first vector.</param>
        /// <returns>The distance between the two vectors.</returns>
        public static float Distance(LVector3 position1, LVector3 position2) => (position1 - position2).Length();

        /// <summary>
        /// Calculates the squared distance between two vectors.
        /// </summary>
        /// <param name="position1">The first vector to calculate the squared distance to the second vector.</param>
        /// <param name="position2">The second vector to calculate the squared distance to the first vector.</param>
        /// <returns>The squared distance between the two vectors.</returns>
        public static float DistanceSquared(LVector3 position1, LVector3 position2) =>
            (position1 - position2).LengthSquared();

        /// <summary>
        /// Calculates the distance between two vectors, ignoring the Z-component.
        /// </summary>
        /// <param name="position1">The first vector to calculate the distance to the second vector.</param>
        /// <param name="position2">The second vector to calculate the distance to the first vector.</param>
        /// <returns>The distance between the two vectors.</returns>
        public static float Distance2D(LVector3 position1, LVector3 position2)
        {
            var pos1 = new LVector3(position1.X, position1.Y, 0);
            var pos2 = new LVector3(position2.X, position2.Y, 0);

            return (pos1 - pos2).Length();
        }

        /// <summary>
        /// Calculates the squared distance between two vectors, ignoring the Z-component.
        /// </summary>
        /// <param name="position1">The first vector to calculate the squared distance to the second vector.</param>
        /// <param name="position2">The second vector to calculate the squared distance to the first vector.</param>
        /// <returns>The squared distance between the two vectors.</returns>
        public static float DistanceSquared2D(LVector3 position1, LVector3 position2)
        {
            var pos1 = new LVector3(position1.X, position1.Y, 0);
            var pos2 = new LVector3(position2.X, position2.Y, 0);

            return (pos1 - pos2).LengthSquared();
        }

        /// <summary>
        /// Returns the angle in degrees between from and to.
        /// The angle returned is always the acute angle between the two vectors.
        /// </summary>
        public static float Angle(LVector3 from, LVector3 to)
        {
            double dot = Dot(from.Normalized, to.Normalized);
            return (float)(System.Math.Acos((dot)) * (180.0 / System.Math.PI));
        }

        /// <summary>
        /// Returns the signed angle in degrees between from and to.
        /// </summary>
        public static float SignedAngle(LVector3 from, LVector3 to, LVector3 planeNormal)
        {
            LVector3 perpVector = Cross(planeNormal, from);

            double angle = Angle(from, to);
            double dot = Dot(perpVector, to);
            if (dot < 0)
            {
                angle *= -1;
            }

            return (float)angle;
        }

        /// <summary>
        /// Converts a vector to a heading.
        /// </summary>
        public float ToHeading() => (float)((System.Math.Atan2(X, -Y) + System.Math.PI) * (180.0 / System.Math.PI));

        /// <summary>
        /// Creates a random vector inside the circle around this position.
        /// </summary>
        public LVector3 Around(float distance) => this + (RandomXY() * distance);

        /// <summary>
        /// Rounds each float inside the vector to a select amount of decimal places (2 by default).
        /// </summary>
        /// <param name="decimalPlaces">Number of decimal places to round to</param>
        /// <returns>The vector containing rounded values</returns>
        public LVector3 Round(int decimalPlaces = 2)
        {
            return new LVector3((float)System.Math.Round(X, decimalPlaces), (float)System.Math.Round(Y, decimalPlaces),
                (float)System.Math.Round(Z, decimalPlaces));
        }

        /// <summary>
        /// Returns a new normalized vector with random X and Y components.
        /// </summary>
        public static LVector3 RandomXY()
        {
            LVector3 v = Zero;
            double radian = CoreUtils.SafeRandom.NextDouble() * 2 * System.Math.PI;

            v.X = (float)(System.Math.Cos(radian));
            v.Y = (float)(System.Math.Sin(radian));
            v.Normalize();
            return v;
        }

        /// <summary>
        /// Returns a new normalized vector with random X, Y and Z components.
        /// </summary>
        public static LVector3 RandomXYZ()
        {
            LVector3 v = Zero;
            double radian = CoreUtils.SafeRandom.NextDouble() * 2.0 * System.Math.PI;
            double cosTheta = (CoreUtils.SafeRandom.NextDouble() * 2.0) - 1.0;
            double theta = System.Math.Acos(cosTheta);

            v.X = (float)(System.Math.Sin(theta) * System.Math.Cos(radian));
            v.Y = (float)(System.Math.Sin(theta) * System.Math.Sin(radian));
            v.Z = (float)(System.Math.Cos(theta));
            v.Normalize();
            return v;
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="left">The first vector to add.</param>
        /// <param name="right">The second vector to add.</param>
        /// <returns>The sum of the two vectors.</returns>
        public static LVector3 Add(LVector3 left, LVector3 right) =>
            new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

        /// <summary>
        /// Subtracts two vectors.
        /// </summary>
        /// <param name="left">The first vector to subtract.</param>
        /// <param name="right">The second vector to subtract.</param>
        /// <returns>The difference of the two vectors.</returns>
        public static LVector3 Subtract(LVector3 left, LVector3 right) =>
            new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

        /// <summary>
        /// Scales a vector by the given value.
        /// </summary>
        /// <param name="value">The vector to scale.</param>
        /// <param name="scale">The amount by which to scale the vector.</param>
        /// <returns>The scaled vector.</returns>
        public static LVector3 Multiply(LVector3 value, float scale) =>
            new(value.X * scale, value.Y * scale, value.Z * scale);

        /// <summary>
        /// Multiply a vector with another by performing component-wise multiplication.
        /// </summary>
        /// <param name="left">The first vector to multiply.</param>
        /// <param name="right">The second vector to multiply.</param>
        /// <returns>The multiplied vector.</returns>
        public static LVector3 Multiply(LVector3 left, LVector3 right) =>
            new(left.X * right.X, left.Y * right.Y, left.Z * right.Z);

        /// <summary>
        /// Scales a vector by the given value.
        /// </summary>
        /// <param name="value">The vector to scale.</param>
        /// <param name="scale">The amount by which to scale the vector.</param>
        /// <returns>The scaled vector.</returns>
        public static LVector3 Divide(LVector3 value, float scale) =>
            new(value.X / scale, value.Y / scale, value.Z / scale);

        /// <summary>
        /// Reverses the direction of a given vector.
        /// </summary>
        /// <param name="value">The vector to negate.</param>
        /// <returns>A vector facing in the opposite direction.</returns>
        public static LVector3 Negate(LVector3 value) => new(-value.X, -value.Y, -value.Z);

        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>The clamped value.</returns>
        public static LVector3 Clamp(LVector3 value, LVector3 min, LVector3 max)
        {
            float x = value.X;
            x = (x > max.X) ? max.X : x;
            x = (x < min.X) ? min.X : x;

            float y = value.Y;
            y = (y > max.Y) ? max.Y : y;
            y = (y < min.Y) ? min.Y : y;

            float z = value.Z;
            z = (z > max.Z) ? max.Z : z;
            z = (z < min.Z) ? min.Z : z;

            return new LVector3(x, y, z);
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
        public static LVector3 Lerp(LVector3 start, LVector3 end, float amount)
        {
            LVector3 vector = Zero;

            vector.X = start.X + ((end.X - start.X) * amount);
            vector.Y = start.Y + ((end.Y - start.Y) * amount);
            vector.Z = start.Z + ((end.Z - start.Z) * amount);

            return vector;
        }

        /// <summary>
        /// Converts the vector into a unit vector.
        /// </summary>
        /// <param name="vector">The vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
        public static LVector3 Normalize(LVector3 vector)
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
        public static float Dot(LVector3 left, LVector3 right) =>
            (left.X * right.X + left.Y * right.Y + left.Z * right.Z);

        /// <summary>
        /// Calculates the cross product of two vectors.
        /// </summary>
        /// <param name="left">First source vector.</param>
        /// <param name="right">Second source vector.</param>
        /// <returns>The cross product of the two vectors.</returns>
        public static LVector3 Cross(LVector3 left, LVector3 right)
        {
            LVector3 result = Zero;
            result.X = left.Y * right.Z - left.Z * right.Y;
            result.Y = left.Z * right.X - left.X * right.Z;
            result.Z = left.X * right.Y - left.Y * right.X;
            return result;
        }

        /// <summary>
        /// Projects a vector onto another vector.
        /// </summary>
        /// <param name="vector">The vector to project.</param>
        /// <param name="onNormal">Vector to project onto, does not assume it is normalized.</param>
        /// <returns>The projected vector.</returns>
        public static LVector3 Project(LVector3 vector, LVector3 onNormal) =>
            onNormal * Dot(vector, onNormal) / Dot(onNormal, onNormal);

        /// <summary>
        /// Projects a vector onto a plane defined by a normal orthogonal to the plane.
        /// </summary>
        /// <param name="vector">The vector to project.</param>
        /// <param name="planeNormal">Normal of the plane,  does not assume it is normalized.</param>
        /// <returns>The Projection of vector onto plane.</returns>
        public static LVector3 ProjectOnPlane(LVector3 vector, LVector3 planeNormal) =>
            (vector - Project(vector, planeNormal));

        /// <summary>
        /// Returns the reflection of a vector off a surface that has the specified normal.
        /// </summary>
        /// <param name="vector">The vector to project onto the plane.</param>
        /// <param name="normal">Normal of the surface.</param>
        /// <returns>The reflected vector.</returns>
        /// <remarks>Reflect only gives the direction of a reflection off a surface, it does not determine
        /// whether the original vector was close enough to the surface to hit it.</remarks>
        public static LVector3 Reflect(LVector3 vector, LVector3 normal)
        {
            LVector3 result = Zero;
            float dot = ((vector.X * normal.X) + (vector.Y * normal.Y)) + (vector.Z * normal.Z);

            result.X = vector.X - ((2.0f * dot) * normal.X);
            result.Y = vector.Y - ((2.0f * dot) * normal.Y);
            result.Z = vector.Z - ((2.0f * dot) * normal.Z);

            return result;
        }

        /// <summary>
        /// Returns a vector containing the smallest components of the specified vectors.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>A vector containing the smallest components of the source vectors.</returns>
        public static LVector3 Minimize(LVector3 left, LVector3 right)
        {
            LVector3 vector = Zero;
            vector.X = (left.X < right.X) ? left.X : right.X;
            vector.Y = (left.Y < right.Y) ? left.Y : right.Y;
            vector.Z = (left.Z < right.Z) ? left.Z : right.Z;
            return vector;
        }

        /// <summary>
        /// Returns a vector containing the largest components of the specified vectors.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>A vector containing the largest components of the source vectors.</returns>
        public static LVector3 Maximize(LVector3 left, LVector3 right)
        {
            LVector3 vector = Zero;
            vector.X = (left.X > right.X) ? left.X : right.X;
            vector.Y = (left.Y > right.Y) ? left.Y : right.Y;
            vector.Z = (left.Z > right.Z) ? left.Z : right.Z;
            return vector;
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="left">The first vector to add.</param>
        /// <param name="right">The second vector to add.</param>
        /// <returns>The sum of the two vectors.</returns>
        public static LVector3 operator +(LVector3 left, LVector3 right) =>
            new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

        /// <summary>
        /// Subtracts two vectors.
        /// </summary>
        /// <param name="left">The first vector to subtract.</param>
        /// <param name="right">The second vector to subtract.</param>
        /// <returns>The difference of the two vectors.</returns>
        public static LVector3 operator -(LVector3 left, LVector3 right) =>
            new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

        /// <summary>
        /// Reverses the direction of a given vector.
        /// </summary>
        /// <param name="vector">The vector to negate.</param>
        /// <returns>A vector facing in the opposite direction.</returns>
        public static LVector3 operator -(LVector3 vector) => new(-vector.X, -vector.Y, -vector.Z);

        /// <summary>
        /// Scales a vector by the given value.
        /// </summary>
        /// <param name="vector">The vector to scale.</param>
        /// <param name="scale">The amount by which to scale the vector.</param>
        /// <returns>The scaled vector.</returns>
        public static LVector3 operator *(LVector3 vector, float scale) =>
            new(vector.X * scale, vector.Y * scale, vector.Z * scale);

        /// <summary>
        /// Scales a vector by the given value.
        /// </summary>
        /// <param name="vector">The vector to scale.</param>
        /// <param name="scale">The amount by which to scale the vector.</param>
        /// <returns>The scaled vector.</returns>
        public static LVector3 operator *(float scale, LVector3 vector) => vector * scale;

        /// <summary>
        /// Scales a vector by the given value.
        /// </summary>
        /// <param name="vector">The vector to scale.</param>
        /// <param name="scale">The amount by which to scale the vector.</param>
        /// <returns>The scaled vector.</returns>
        public static LVector3 operator /(LVector3 vector, float scale)
        {
            float invScale = 1.0f / scale;
            return new LVector3(vector.X * invScale, vector.Y * invScale, vector.Z * invScale);
        }

        /// <summary>
        /// Tests for equality between two objects.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="left"/> has the same value as <paramref name="right"/>; otherwise, <see langword="false" />.</returns>
        public static bool operator ==(LVector3 left, LVector3 right) => Equals(left, right);

        /// <summary>
        /// Tests for inequality between two objects.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="left"/> has a different value than <paramref name="right"/>; otherwise, <see langword="false" />.</returns>
        public static bool operator !=(LVector3 left, LVector3 right) => !Equals(left, right);

        /// <summary>
        /// Converts a Vector3 to a Vector2 implicitly.
        /// </summary>
        public static implicit operator LVector2(LVector3 vector) => new(vector.X, vector.Y);

        /// <summary>
        /// Converts the matrix to an array of floats.
        /// </summary>
        public float[] ToArray() => new[] { X, Y, Z };

        /// <summary>
        /// Converts the value of the object to its equivalent string representation.
        /// </summary>
        /// <returns>The string representation of the value of this instance.</returns>
        public override string ToString() => string.Format(CultureInfo.CurrentCulture, "X:{0} Y:{1} Z:{2}", X, Y, Z);

        /// <summary>
        /// Converts the value of the object to its equivalent string representation.
        /// </summary>
        /// <param name="format">The number format.</param>
        /// <returns>The string representation of the value of this instance.</returns>
        public string ToString(string format)
        {
            if (format == null)
            {
                return ToString();
            }

            return string.Format(CultureInfo.CurrentCulture, "X:{0} Y:{1} Z:{2}",
                X.ToString(format, CultureInfo.CurrentCulture),
                Y.ToString(format, CultureInfo.CurrentCulture), Z.ToString(format, CultureInfo.CurrentCulture));
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = X.GetHashCode();
                hashCode = (hashCode * 397) ^ Y.GetHashCode();
                hashCode = (hashCode * 397) ^ Z.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Returns a value that indicates whether the current instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">Object to make the comparison with.</param>
        /// <returns><see langword="true" /> if the current instance is equal to the specified object; <see langword="false" /> otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((LVector3)obj);
        }

        /// <summary>
        /// Returns a value that indicates whether the current instance is equal to the specified object.
        /// </summary>
        /// <param name="other">Object to make the comparison with.</param>
        /// <returns><see langword="true" /> if the current instance is equal to the specified object; <see langword="false" /> otherwise.</returns>
        public bool Equals(LVector3 other) => (X == other.X && Y == other.Y && Z == other.Z);

        public static implicit operator LVector3(Vector3 v) => new(v.X, v.Y, v.Z);
        public static implicit operator Vector3(LVector3 v) => new(v.X, v.Y, v.Z);
    }
}
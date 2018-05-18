using System;
using System.Collections;
using System.Collections.Generic;
// ReSharper disable All

#if (! NETSTANDARD2_0)
namespace Unearth
{
    /// <summary>
    /// Represents zero/null, one, or many strings in an efficient way.
    /// </summary>
    public struct StringValues : IList<string>, ICollection<string>, IEnumerable<string>, IEnumerable, IReadOnlyList<string>, IReadOnlyCollection<string>, IEquatable<StringValues>, IEquatable<string>, IEquatable<string[]>
    {
        private static readonly string[] EmptyArray = new string[0];
        public static readonly StringValues Empty = new StringValues(StringValues.EmptyArray);
        private readonly string _value;
        private readonly string[] _values;

        public int Count
        {
            get
            {
                if (this._value != null)
                    return 1;
                string[] values = this._values;
                if (values == null)
                    return 0;
                return values.Length;
            }
        }

        bool ICollection<string>.IsReadOnly
        {
            get
            {
                return true;
            }
        }

        string IList<string>.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public string this[int index]
        {
            get
            {
                if (this._values != null)
                    return this._values[index];
                if (index == 0 && this._value != null)
                    return this._value;
                return StringValues.EmptyArray[0];
            }
        }

        public StringValues(string value)
        {
            this._value = value;
            this._values = (string[])null;
        }

        public StringValues(string[] values)
        {
            this._value = (string)null;
            this._values = values;
        }

        public static implicit operator StringValues(string value)
        {
            return new StringValues(value);
        }

        public static implicit operator StringValues(string[] values)
        {
            return new StringValues(values);
        }

        public static implicit operator string(StringValues values)
        {
            return values.GetStringValue();
        }

        public static implicit operator string[] (StringValues value)
        {
            return value.GetArrayValue();
        }

        public static bool operator ==(StringValues left, StringValues right)
        {
            return StringValues.Equals(left, right);
        }

        public static bool operator !=(StringValues left, StringValues right)
        {
            return !StringValues.Equals(left, right);
        }

        public static bool operator ==(StringValues left, string right)
        {
            return StringValues.Equals(left, new StringValues(right));
        }

        public static bool operator !=(StringValues left, string right)
        {
            return !StringValues.Equals(left, new StringValues(right));
        }

        public static bool operator ==(string left, StringValues right)
        {
            return StringValues.Equals(new StringValues(left), right);
        }

        public static bool operator !=(string left, StringValues right)
        {
            return !StringValues.Equals(new StringValues(left), right);
        }

        public static bool operator ==(StringValues left, string[] right)
        {
            return StringValues.Equals(left, new StringValues(right));
        }

        public static bool operator !=(StringValues left, string[] right)
        {
            return !StringValues.Equals(left, new StringValues(right));
        }

        public static bool operator ==(string[] left, StringValues right)
        {
            return StringValues.Equals(new StringValues(left), right);
        }

        public static bool operator !=(string[] left, StringValues right)
        {
            return !StringValues.Equals(new StringValues(left), right);
        }

        public static bool operator ==(StringValues left, object right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StringValues left, object right)
        {
            return !left.Equals(right);
        }

        public static bool operator ==(object left, StringValues right)
        {
            return right.Equals(left);
        }

        public static bool operator !=(object left, StringValues right)
        {
            return !right.Equals(left);
        }

        public override string ToString()
        {
            return this.GetStringValue() ?? string.Empty;
        }

        private string GetStringValue()
        {
            if (this._values == null)
                return this._value;
            switch (this._values.Length)
            {
                case 0:
                    return (string)null;
                case 1:
                    return this._values[0];
                default:
                    return string.Join(",", this._values);
            }
        }

        public string[] ToArray()
        {
            return this.GetArrayValue() ?? StringValues.EmptyArray;
        }

        private string[] GetArrayValue()
        {
            if (this._value == null)
                return this._values;
            return new string[1] { this._value };
        }

        int IList<string>.IndexOf(string item)
        {
            return this.IndexOf(item);
        }

        private int IndexOf(string item)
        {
            if (this._values != null)
            {
                string[] values = this._values;
                for (int index = 0; index < values.Length; ++index)
                {
                    if (string.Equals(values[index], item, StringComparison.Ordinal))
                        return index;
                }
                return -1;
            }
            return this._value != null && string.Equals(this._value, item, StringComparison.Ordinal) ? 0 : -1;
        }

        bool ICollection<string>.Contains(string item)
        {
            return this.IndexOf(item) >= 0;
        }

        void ICollection<string>.CopyTo(string[] array, int arrayIndex)
        {
            this.CopyTo(array, arrayIndex);
        }

        private void CopyTo(string[] array, int arrayIndex)
        {
            if (this._values != null)
            {
                Array.Copy((Array)this._values, 0, (Array)array, arrayIndex, this._values.Length);
            }
            else
            {
                if (this._value == null)
                    return;
                if (array == null)
                    throw new ArgumentNullException("array");
                if (arrayIndex < 0)
                    throw new ArgumentOutOfRangeException("arrayIndex");
                if (array.Length - arrayIndex < 1)
                    throw new ArgumentException(string.Format("'{0}' is not long enough to copy all the items in the collection. Check '{1}' and '{2}' length.", new object[3]
                    {
                        (object) "array",
                        (object) "arrayIndex",
                        (object) "array"
                    }));
                array[arrayIndex] = this._value;
            }
        }

        void ICollection<string>.Add(string item)
        {
            throw new NotSupportedException();
        }

        void IList<string>.Insert(int index, string item)
        {
            throw new NotSupportedException();
        }

        bool ICollection<string>.Remove(string item)
        {
            throw new NotSupportedException();
        }

        void IList<string>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        void ICollection<string>.Clear()
        {
            throw new NotSupportedException();
        }

        public StringValues.Enumerator GetEnumerator()
        {
            return new StringValues.Enumerator(ref this);
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return (IEnumerator<string>)this.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)this.GetEnumerator();
        }

        public static bool IsNullOrEmpty(StringValues value)
        {
            if (value._values == null)
                return string.IsNullOrEmpty(value._value);
            switch (value._values.Length)
            {
                case 0:
                    return true;
                case 1:
                    return string.IsNullOrEmpty(value._values[0]);
                default:
                    return false;
            }
        }

        public static StringValues Concat(StringValues values1, StringValues values2)
        {
            int count1 = values1.Count;
            int count2 = values2.Count;
            if (count1 == 0)
                return values2;
            if (count2 == 0)
                return values1;
            string[] strArray = new string[count1 + count2];
            values1.CopyTo(strArray, 0);
            values2.CopyTo(strArray, count1);
            return new StringValues(strArray);
        }

        public static bool Equals(StringValues left, StringValues right)
        {
            int count = left.Count;
            if (count != right.Count)
                return false;
            for (int index = 0; index < count; ++index)
            {
                if (left[index] != right[index])
                    return false;
            }
            return true;
        }

        public bool Equals(StringValues other)
        {
            return StringValues.Equals(this, other);
        }

        public static bool Equals(string left, StringValues right)
        {
            return StringValues.Equals(new StringValues(left), right);
        }

        public static bool Equals(StringValues left, string right)
        {
            return StringValues.Equals(left, new StringValues(right));
        }

        public bool Equals(string other)
        {
            return StringValues.Equals(this, new StringValues(other));
        }

        public static bool Equals(string[] left, StringValues right)
        {
            return StringValues.Equals(new StringValues(left), right);
        }

        public static bool Equals(StringValues left, string[] right)
        {
            return StringValues.Equals(left, new StringValues(right));
        }

        public bool Equals(string[] other)
        {
            return StringValues.Equals(this, new StringValues(other));
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return StringValues.Equals(this, StringValues.Empty);
            if (obj is string)
                return StringValues.Equals(this, (string)obj);
            if (obj is string[])
                return StringValues.Equals(this, (string[])obj);
            if (obj is StringValues)
                return StringValues.Equals(this, (StringValues)obj);
            return false;
        }

        public override int GetHashCode()
        {
            if (this._values == null)
            {
                if (this._value != null)
                    return this._value.GetHashCode();
                return 0;
            }

            // this differs from the M$ implementation, has no dependencies
            return string.Join("|", _values).GetHashCode();
        }

        public struct Enumerator : IEnumerator<string>, IEnumerator, IDisposable
        {
            private readonly string[] _values;
            private string _current;
            private int _index;

            public string Current
            {
                get
                {
                    return this._current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return (object)this._current;
                }
            }

            public Enumerator(ref StringValues values)
            {
                this._values = values._values;
                this._current = values._value;
                this._index = 0;
            }

            public bool MoveNext()
            {
                if (this._index < 0)
                    return false;
                if (this._values != null)
                {
                    if (this._index < this._values.Length)
                    {
                        this._current = this._values[this._index];
                        this._index = this._index + 1;
                        return true;
                    }
                    this._index = -1;
                    return false;
                }
                this._index = -1;
                return this._current != null;
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            void IDisposable.Dispose()
            {
            }
        }
    }
}

namespace Microsoft.Extensions.Primitives
{
    // This prevents compile errors - don't delete it in the NetFX project
}
#endif
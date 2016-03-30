using System;
using System.Text;
using Newtonsoft.Json;

namespace CKAN.Versioning
{
    /// <summary>
    /// Represents the version number of a Kerbal Space Program (KSP) installation.
    /// </summary>
    [JsonConverter(typeof(KspVersionJsonConverter))]
    public sealed partial class KspVersion
    {
        private const int Undefined = -1;

        private readonly int _major;
        private readonly int _minor;
        private readonly int _patch;
        private readonly int _build;

        private readonly string _string;

        /// <summary>
        /// Gets the value of the major component of the version number for the current <see cref="KspVersion"/>
        /// object.
        /// </summary>
        public int Major {  get { return _major; } }

        /// <summary>
        /// Gets the value of the minor component of the version number for the current <see cref="KspVersion"/>
        /// object.
        /// </summary>
        public int Minor { get { return _minor; } }

        /// <summary>
        /// Gets the value of the patch component of the version number for the current <see cref="KspVersion"/>
        /// object.
        /// </summary>
        public int Patch { get { return _patch; } }

        /// <summary>
        /// Gets the value of the build component of the version number for the current <see cref="KspVersion"/>
        /// object.
        /// </summary>
        public int Build { get { return _build; } }

        /// <summary>
        /// Gets whether or not the major component of the version number for the current <see cref="KspVersion"/>
        /// object is defined.
        /// </summary>
        public bool IsMajorDefined { get { return _major != Undefined; } }

        /// <summary>
        /// Gets whether or not the minor component of the version number for the current <see cref="KspVersion"/>
        /// object is defined.
        /// </summary>
        public bool IsMinorDefined { get { return _minor != Undefined; } }


        /// <summary>
        /// Gets whether or not the patch component of the version number for the current <see cref="KspVersion"/>
        /// object is defined.
        /// </summary>
        public bool IsPatchDefined { get { return _patch != Undefined; } }


        /// <summary>
        /// Gets whether or not the build component of the version number for the current <see cref="KspVersion"/>
        /// object is defined.
        /// </summary>
        public bool IsBuildDefined {  get { return _build != Undefined; } }

        /// <summary>
        /// Indicates whether or not all components of the current <see cref="KspVersion"/> are defined.
        /// </summary>
        public bool IsFullyDefined
        {
            get { return IsMajorDefined && IsMinorDefined && IsPatchDefined && IsBuildDefined; }
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="KspVersion"/> class with all components unspecified.
        /// </summary>
        public KspVersion()
        {
            _major = Undefined;
            _minor = Undefined;
            _patch = Undefined;
            _build = Undefined;

            _string = DeriveString(_major, _minor, _patch, _build);
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="KspVersion"/> class using the specified major value.
        /// </summary>
        /// <param name="major">The major version number.</param>
        public KspVersion(int major)
        {
            if (major < 0)
                throw new ArgumentOutOfRangeException("major");

            _major = major;
            _minor = Undefined;
            _patch = Undefined;
            _build = Undefined;

            _string = DeriveString(_major, _minor, _patch, _build);
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="KspVersion"/> class using the specified major and minor
        /// values.
        /// </summary>
        /// <param name="major">The major version number.</param>
        /// <param name="minor">The minor version number.</param>
        public KspVersion(int major, int minor)
        {
            if (major < 0)
                throw new ArgumentOutOfRangeException("major");

            if (minor < 0)
                throw new ArgumentOutOfRangeException("minor");

            _major = major;
            _minor = minor;
            _patch = Undefined;
            _build = Undefined;

            _string = DeriveString(_major, _minor, _patch, _build);
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="KspVersion"/> class using the specified major, minor, and
        /// patch values.
        /// </summary>
        /// <param name="major">The major version number.</param>
        /// <param name="minor">The minor version number.</param>
        /// <param name="patch">The patch version number.</param>
        public KspVersion(int major, int minor, int patch)
        {
            if (major < 0)
                throw new ArgumentOutOfRangeException("major");

            if (minor < 0)
                throw new ArgumentOutOfRangeException("minor");

            if (patch < 0)
                throw new ArgumentOutOfRangeException("patch");

            _major = major;
            _minor = minor;
            _patch = patch;
            _build = Undefined;

            _string = DeriveString(_major, _minor, _patch, _build);
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="KspVersion"/> class using the specified major, minor, patch,
        /// and build values.
        /// </summary>
        /// <param name="major">The major version number.</param>
        /// <param name="minor">The minor version number.</param>
        /// <param name="patch">The patch version number.</param>
        /// <param name="build">The build verison number.</param>
        public KspVersion(int major, int minor, int patch, int build)
        {
            if (major < 0)
                throw new ArgumentOutOfRangeException("major");

            if (minor < 0)
                throw new ArgumentOutOfRangeException("minor");

            if (patch < 0)
                throw new ArgumentOutOfRangeException("patch");

            if (build < 0)
                throw new ArgumentOutOfRangeException("build");

            _major = major;
            _minor = minor;
            _patch = patch;
            _build = build;

            _string = DeriveString(_major, _minor, _patch, _build);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KspVersion"/> class using the specified string.
        /// </summary>
        /// <param name="version">
        /// A string containing the major, minor, patch, and build numbers, where each number is deliminted with a
        /// period character ('.').
        /// </param>
        public KspVersion(string version)
        {
            int[] components;
            switch (TryParseComponents(version, out components))
            {
                case ParseStatus.Success:
                    _major = components[0];
                    _minor = components[1];
                    _patch = components[2];
                    _build = components[3];

                    _string = DeriveString(_major, _minor, _patch, _build);
                    break;
                case ParseStatus.NullInput:
                    throw new ArgumentNullException("version");
                case ParseStatus.TooManyComponents:
                    throw new ArgumentException("Version string portion was too long.");
                case ParseStatus.NonIntegerComponent:
                    throw new FormatException("Input string was not in a correct format.");
                case ParseStatus.NegativeComponent:
                    throw new ArgumentOutOfRangeException(
                        "version",
                        "Version's parameters must be greater than or equal to zero."
                    );
                case ParseStatus.TooLargeComponent:
                    throw new OverflowException("Value was either too large or too small for an Int32");
                default:
                    throw new ArgumentOutOfRangeException();
            }                
        }

        /// <summary>
        /// Converts the value of the current <see cref="KspVersion"/> to its equivalent <see cref="String"/>
        /// representation.
        /// </summary>
        /// <returns>
        /// <para>
        /// The <see cref="String"/> representation of the values of the major, minor, patch, and build components of
        /// the current <see cref="KspVersion"/> object as depicted in the following format. Each component is
        /// separated by a period character ('.'). Square brackets ('[' and ']') indicate that will not appear in the
        /// return value if the component is not defined:
        /// </para>
        /// <para>
        /// [<i>major</i>[.<i>minor</i>[.<i>patch</i>[.<i>build</i>]]]]
        /// </para>
        /// <para>
        /// For example, if you create a <see cref="KspVersion"/> object using the constructor <c>KspVersion(1,1)</c>,
        /// the returned string is "1.1". If you create a <see cref="KspVersion"/> using the constructor (1,3,4,2),
        /// the returned string is "1.3.4.2".
        /// </para>
        /// </returns>
        public override string ToString()
        {
            return _string;
        }

        /// <summary>
        /// Converts the value of the current <see cref="KspVersion"/> to its equivalent
        /// <see cref="KspVersionRange"/>.
        /// </summary>
        /// <returns>
        /// <para>
        /// A <see cref="KspVersionRange"/> which specifies a set of versions equivalent to the current
        /// <see cref="KspVersion"/>.
        /// </para>
        /// <para>
        /// For example, the version "1.0.0.0" would be equivalent to the range ["1.0.0.0", "1.0.0.0"], while the
        /// version "1.0" would be equivalent to the range ["1.0.0.0", "1.1.0.0"). Where '[' and ']' represent 
        /// includisve bounds and '(' and ')' represent exclusive bounds.
        /// </para>
        /// </returns>
        public KspVersionRange ToVersionRange()
        {
            KspVersionBound lower;
            KspVersionBound upper;
        
            if (IsBuildDefined)
            {
                lower = new KspVersionBound(this, inclusive: true);
                upper = new KspVersionBound(this, inclusive: true);
            }
            else if (IsPatchDefined)
            {
                lower = new KspVersionBound(new KspVersion(Major, Minor, Patch, 0), inclusive: true);
                upper = new KspVersionBound(new KspVersion(Major, Minor, Patch + 1, 0), inclusive: false);
            }
            else if (IsMinorDefined)
            {
                lower = new KspVersionBound(new KspVersion(Major, Minor, 0, 0), inclusive: true);
                upper = new KspVersionBound(new KspVersion(Major, Minor + 1, 0, 0), inclusive: false);
            }
            else if (IsMajorDefined)
            {
                lower = new KspVersionBound(new KspVersion(Major, 0, 0, 0), inclusive: true);
                upper = new KspVersionBound(new KspVersion(Major + 1, 0, 0, 0), inclusive: false);
            }
            else
            {
                lower = new KspVersionBound();
                upper = new KspVersionBound();
            }

            return new KspVersionRange(lower, upper);
        }

        /// <summary>
        /// Covnerts the string representation of a verison number to an equivalent <see cref="KspVersion"/> object.
        /// </summary>
        /// <param name="input">A string that contains a version number to convert.</param>
        /// <returns>
        /// A <see cref="KspVersion"/> object that is equivalent to the version number specified in the
        /// <see cref="input"/> parameter.
        /// </returns>
        public static KspVersion Parse(string input)
        {
            return new KspVersion(input);
        }

        /// <summary>
        /// Tries to convert the string representation of a version number to an equivalent <see cref="KspVersion"/>
        /// object and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="input">
        /// A string that contains a version number to convert.
        /// </param>
        /// <param name="result">
        /// When this method returns, contains the <see cref="KspVersion"/> equivalent of the number that is contained
        /// in <see cref="input"/>, if the conversion succeeded, or a <see cref="KspVersion"/> object whose components
        /// are undefined if the conversion failed. If <see cref="input"/> is <c>null</c> or
        /// <see cref="string.Empty"/>, <see cref="result"/> is <c>null</c> when the method returns.
        /// </param>
        /// <returns>
        /// <c>true</c> if the <see cref="input"/> parameter was converted successfully; otherwise, <c>false</c>.
        /// </returns>
        public static bool TryParse(string input, out KspVersion result)
        {
            if (ReferenceEquals(input, null) || input.Equals(string.Empty))
            {
                result = null;
                return false;
            }
            else
            {
                int[] components;
                switch (TryParseComponents(input, out components))
                {
                    case ParseStatus.Success:
                        var major = components[0];
                        var minor = components[1];
                        var patch = components[2];
                        var build = components[3];

                        if (major == Undefined)
                            result = new KspVersion();
                        else if (minor == Undefined)
                            result = new KspVersion(major);
                        else if (patch == Undefined)
                            result = new KspVersion(major, minor);
                        else if (build == Undefined)
                            result = new KspVersion(major, minor, patch);
                        else
                            result = new KspVersion(major, minor, patch, build);

                        return true;
                    default:
                        result = new KspVersion();
                        return false;
                }
            }
        }
    }

    public sealed partial class KspVersion : IEquatable<KspVersion>
    {
        /// <summary>
        /// Returns a value indicating whether the current <see cref="KspVersion"/> object and specified
        /// <see cref="KspVersion"/> object represent the same value.
        /// </summary>
        /// <param name="obj">
        /// A <see cref="KspVersion"/> object to compare to the current <see cref="KspVersion"/> object, or
        /// <c>null</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if every component of the current <see cref="KspVersion"/> matches the corresponding component
        /// of the <see cref="obj"/> parameter; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(KspVersion obj)
        {
            if (ReferenceEquals(obj, null)) return false;
            if (ReferenceEquals(obj, this)) return true;
            return _major == obj._major && _minor == obj._minor && _patch == obj._patch && _build == obj._build;
        }

        /// <summary>
        /// Returns a value indicating whether the current <see cref="KspVersion"/> object is equal to a specified
        /// object.
        /// </summary>
        /// <param name="obj">
        /// An object to compare with the current <see cref="KspVersion"/> object, or <c>null</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the current <see cref="KspVersion"/> object and <see cref="obj"/> are both
        /// <see cref="KspVersion"/> objects and every component of the current <see cref="KspVersion"/> object
        /// matches the corresponding component of <see cref="obj"/>; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null)) return false;
            if (ReferenceEquals(obj, this)) return true;
            return obj is KspVersion && Equals((KspVersion) obj);
        }

        /// <summary>
        /// Returns a hash code for the current <see cref="KspVersion"/> object.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _major.GetHashCode();
                hashCode = (hashCode*397) ^ _minor.GetHashCode();
                hashCode = (hashCode*397) ^ _patch.GetHashCode();
                hashCode = (hashCode*397) ^ _build.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Determines whether two specified <see cref="KspVersion"/> objects are equal.
        /// </summary>
        /// <param name="v1">The first <see cref="KspVersion"/> object.</param>
        /// <param name="v2">The second <see cref="KspVersion"/> object.</param>
        /// <returns><c>true</c> if <see cref="v1"/> equals <see cref="v2"/>; otherwise, <c>false</c>.</returns>
        public static bool operator ==(KspVersion v1, KspVersion v2)
        {
            return Equals(v1, v2);
        }

        /// <summary>
        /// Determines whether two specified <see cref="KspVersion"/> objects are not equal.
        /// </summary>
        /// <param name="v1">The first <see cref="KspVersion"/> object.</param>
        /// <param name="v2">The second <see cref="KspVersion"/> object.</param>
        /// <returns>
        /// <c>true</c> if <see cref="v1"/> does not equal <see cref="v2"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator !=(KspVersion v1, KspVersion v2)
        {
            return !Equals(v1, v2);
        }
    }

    public sealed partial class KspVersion : IComparable, IComparable<KspVersion>
    {
        /// <summary>
        /// Compares the current <see cref="KspVersion"/> object to a specified object and returns an indication of
        /// their relative values.
        /// </summary>
        /// <param name="value">An object to compare, or <c>null</c>.</param>
        /// <returns>
        /// A signed integer that indicates the relative values of the two objects, as shown in the following table.
        /// <list type="table">
        /// <listheader>
        /// <term>Return value</term>
        /// <description>Meaning</description>
        /// </listheader>
        /// <item>
        /// <term>Less than zero</term>
        /// <description>
        /// The current <see cref="KspVersion"/> object is a version before <see cref="value"/>.
        /// </description>
        /// </item>
        /// <item>
        /// <term>Zero</term>
        /// <description>
        /// The current <see cref="KspVersion"/> object is the same version as <see cref="value"/>.
        /// </description>
        /// </item>
        /// <item>
        /// <term>Greater than zero</term>
        /// <description>
        /// <para>
        /// The current <see cref="KspVersion"/> object is a version subsequent to <see cref="value"/>.
        /// </para>
        /// <para>
        /// -or-
        /// </para>
        /// <para>
        /// <see cref="value"/> is <c>null</c>.
        /// </para>
        /// </description>
        /// </item>
        /// </list>
        /// </returns>
        public int CompareTo(KspVersion value)
        {
            if (Equals(this, value))
                return 0;

            if (ReferenceEquals(value, null))
                return 1;

            var majorCompare = _major.CompareTo(value._major);

            if (majorCompare == 0)
            {
                var minorCompare = _minor.CompareTo(value._minor);

                if (minorCompare == 0)
                {
                    var patchCompare = _patch.CompareTo(value._patch);

                    if (patchCompare == 0)
                    {
                        return _build.CompareTo(value._build);
                    }
                    else
                    {
                        return patchCompare;
                    }
                }
                else
                {
                    return minorCompare;
                }
            }
            else
            {
                return majorCompare;
            }
        }

        /// <summary>
        /// Compares the current <see cref="KspVersion"/> object to a specified object and returns an indication of
        /// their relative values.
        /// </summary>
        /// <param name="version">An object to compare, or <c>null</c>.</param>
        /// <returns>
        /// A signed integer that indicates the relative values of the two objects, as shown in the following table.
        /// <list type="table">
        /// <listheader>
        /// <term>Return value</term>
        /// <description>Meaning</description>
        /// </listheader>
        /// <item>
        /// <term>Less than zero</term>
        /// <description>
        /// The current <see cref="KspVersion"/> object is a version before <see cref="version"/>.
        /// </description>
        /// </item>
        /// <item>
        /// <term>Zero</term>
        /// <description>
        /// The current <see cref="KspVersion"/> object is the same version as <see cref="version"/>.
        /// </description>
        /// </item>
        /// <item>
        /// <term>Greater than zero</term>
        /// <description>
        /// <para>
        /// The current <see cref="KspVersion"/> object is a version subsequent to <see cref="version"/>.
        /// </para>
        /// <para>
        /// -or-
        /// </para>
        /// <para>
        /// <see cref="version"/> is <c>null</c>.
        /// </para>
        /// </description>
        /// </item>
        /// </list>
        /// </returns>
        public int CompareTo(object version)
        {
            var objKspVersion = version as KspVersion;

            if (objKspVersion != null)
                return CompareTo(objKspVersion);
            else
                throw new ArgumentException("Object must be of type KspVersion.");
        }

        /// <summary>
        /// Determines whether the first specified <see cref="KspVersion"/> object is less than the second specified
        /// <see cref="KspVersion"/> object.
        /// </summary>
        /// <param name="v1">The first <see cref="KspVersion"/> object.</param>
        /// <param name="v2">The second <see cref="KspVersion"/> object.</param>
        /// <returns><c>true</c> if <see cref="v1"/> is less than <see cref="v2"/>; otherwise, <c>flase</c>.</returns>
        public static bool operator <(KspVersion v1, KspVersion v2)
        {
            if (ReferenceEquals(v1, null))
                throw new ArgumentNullException("v1");

            return v1.CompareTo(v2) < 0;
        }

        /// <summary>
        /// Determines whether the first specified <see cref="KspVersion"/> object is greater than the second
        /// specified <see cref="Version"/> object.
        /// </summary>
        /// <param name="v1">The first <see cref="KspVersion"/> object.</param>
        /// <param name="v2">The second <see cref="KspVersion"/> object.</param>
        /// <returns>
        /// <c>true</c> if <see cref="v1"/> is greater than <see cref="v2"/>; otherwise, <c>flase</c>.
        /// </returns>
        public static bool operator >(KspVersion v1, KspVersion v2)
        {
            if (ReferenceEquals(v1, null))
                throw new ArgumentNullException("v1");

            return v1.CompareTo(v2) > 0;
        }

        /// <summary>
        /// Determines whether the first specified <see cref="KspVersion"/> object is less than or equal to the second
        /// specified <see cref="KspVersion"/> object.
        /// </summary>
        /// <param name="v1">The first <see cref="KspVersion"/> object.</param>
        /// <param name="v2">The second <see cref="KspVersion"/> object.</param>
        /// <returns>
        /// <c>true</c> if <see cref="v1"/> is less than or equal to <see cref="v2"/>; otherwise, <c>flase</c>.
        /// </returns>
        public static bool operator <=(KspVersion v1, KspVersion v2)
        {
            if (ReferenceEquals(v1, null))
                throw new ArgumentNullException("v1");

            return v1.CompareTo(v2) <= 0;
        }

        /// <summary>
        /// Determines whether the first specified <see cref="KspVersion"/> object is greater than or equal to the
        /// second specified <see cref="KspVersion"/> object.
        /// </summary>
        /// <param name="v1">The first <see cref="KspVersion"/> object.</param>
        /// <param name="v2">The second <see cref="KspVersion"/> object.</param>
        /// <returns>
        /// <c>true</c> if <see cref="v1"/> is greater than or equal to <see cref="v2"/>; otherwise, <c>flase</c>.
        /// </returns>
        public static bool operator >=(KspVersion v1, KspVersion v2)
        {
            if (ReferenceEquals(v1, null))
                throw new ArgumentNullException("v1");

            return v1.CompareTo(v2) >= 0;
        }
    }

    public sealed partial class KspVersion
    {
        private static ParseStatus TryParseComponents(string version, out int[] components)
        {
            components = new[] { -1, -1, -1, -1 };

            if (ReferenceEquals(version, null))
                return ParseStatus.NullInput;

            if (version.Trim() != string.Empty)
            {
                var stringComponents = version.Trim().Split('.');

                if (stringComponents.Length > 4)
                    return ParseStatus.TooManyComponents;

                for (var i = 0; i < stringComponents.Length; i++)
                {
                    long result;
                    if (long.TryParse(stringComponents[i], out result))
                    {
                        if (result < 0)
                            return ParseStatus.NegativeComponent;
                        else if (result > int.MaxValue)
                            return ParseStatus.TooLargeComponent;
                        else
                            components[i] = (int)result;
                    }
                    else
                    {
                        return ParseStatus.NonIntegerComponent;
                    }
                }
            }

            return ParseStatus.Success;
        }

        private static string DeriveString(int major, int minor, int patch, int build)
        {
            var sb = new StringBuilder();

            if (major != Undefined)
            {
                sb.Append(major);
            }

            if (minor != Undefined)
            {
                sb.Append(".");
                sb.Append(minor);
            }

            if (patch != Undefined)
            {
                sb.Append(".");
                sb.Append(patch);
            }

            if (build != Undefined)
            {
                sb.Append(".");
                sb.Append(build);
            }

            return sb.ToString();
        }

        private enum ParseStatus
        {
            Success             = +1,
            NullInput           = -1,
            TooManyComponents   = -2,
            NonIntegerComponent = -3,
            NegativeComponent   = -4,
            TooLargeComponent   = -5,
        }
    }

    public sealed class KspVersionJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var value = reader.Value == null ? null : reader.Value.ToString();

            if (value == null)
            {
                return null;
            }
            else if (value == "any")
            {
                return new KspVersion();
            }
            else
            {
                KspVersion result;
                if (KspVersion.TryParse(value, out result))
                {
                    return result;
                }
                else
                {
                    throw new JsonException(string.Format("Could not parse KSP version: {0}", value));
                }
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(KspVersion);
        }
    }
}

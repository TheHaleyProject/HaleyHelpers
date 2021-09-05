using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Haley.Models;
using System.Reflection;
using System.Data;

namespace Haley.Utils
{
    public delegate bool CustomTypeConverter(PropertyInfo target_prop, object source_value, out object converted_value);
    public static class GeneralUtils
    {
        private static Random rand = new Random();
        public static bool getRandomBool()
        {
            if (rand.Next(0, 2) == 0) //Number will be betweewn 0 and 1.
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static int getRandomZeroOne()
        {
            return rand.Next(0, 2);
        }
        /// <summary>
        /// Fill properties
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="include_ignored_properties">Properties which has IgnoreMapping Attribute</param>
        public static TTarget MapProperties<TSource, TTarget>(TSource source, TTarget target, bool include_ignored_properties = false, bool ignore_case = false, CustomTypeConverter typeParser = null)
            where TSource : class
            where TTarget : class
        {
            try
            {
                //Sometimes target can be null. We can set it default.
                if (target == null || source == null)
                {
                    return target; //Then there is nothing to map.
                }

                var sourceProperties = source.GetType().GetProperties().Select(p => p); //We don't have to worry about source properites being readonly. We are merely going to get it.
                var targetProperties = target.GetType().GetProperties().Where(p => p.CanWrite); //Only take properties which are not readonly.

                if (sourceProperties == null || sourceProperties?.Count() == 0 || targetProperties == null || targetProperties?.Count() == 0) return target;

                StringComparison _comparisionMethod = StringComparison.InvariantCulture;
                if (ignore_case)
                {
                    _comparisionMethod = StringComparison.InvariantCultureIgnoreCase;
                }

                #region Remove Ignored Properties
                if (!include_ignored_properties)
                {
                    List<PropertyInfo> _toremoveSource = new List<PropertyInfo>();
                    List<PropertyInfo> _toremoveTarget = new List<PropertyInfo>();

                    //Filter source. (Using Linq increases process time. So going with foreach)
                    foreach (var prop in sourceProperties.Where(p => Attribute.IsDefined(p, typeof(IgnoreMappingAttribute))))
                    {
                        var _ignoreAttribute = prop.GetCustomAttribute<IgnoreMappingAttribute>();
                        if (_ignoreAttribute == null) continue;
                        var _mode = _ignoreAttribute.Mode;
                        if (_mode == IgnoreMappingMode.Both || _mode == IgnoreMappingMode.FromThisObject)
                        {
                            _toremoveSource.Add(prop); //Only if both or from this object is ignored.
                        }
                    }

                    //Filter target
                    foreach (var prop in targetProperties.Where(p => Attribute.IsDefined(p, typeof(IgnoreMappingAttribute))))
                    {
                        var _ignoreAttribute = prop.GetCustomAttribute<IgnoreMappingAttribute>();
                        if (_ignoreAttribute == null) continue;
                        var _mode = _ignoreAttribute.Mode;
                        if (_mode == IgnoreMappingMode.Both || _mode == IgnoreMappingMode.ToThisObject)
                        {
                            _toremoveTarget.Add(prop); //Only if both or from this object is ignored.
                        }
                    }

                    //Remove properties in source which has "Both" & "FromThis" ignore properties
                    sourceProperties = sourceProperties.Except(_toremoveSource);
                    targetProperties = targetProperties.Except(_toremoveTarget);
                }
                #endregion

                foreach (var targetProp in targetProperties)
                {
                    //Getting only for the target (not for the source).
                    var possibleNameMatches = _getOtherNames(targetProp);
                    object targetValue = null;

                    foreach (var sourceProp in sourceProperties)
                    {
                        object sourcePropValue = null;
                        var _sourceName = sourceProp.Name;

                        if (possibleNameMatches.Any(p => p.Equals(_sourceName, _comparisionMethod))) //In any case, names should match
                        {
                            sourcePropValue = sourceProp.GetValue(source);

                            if (typeParser != null && typeParser.Invoke(targetProp, sourcePropValue, out object _convertedval))
                            {
                                //Sometimes before mapping, we might want to do some processing for certain properties. So, using delegate.
                                if (_convertedval != null && _convertedval.GetType() == targetProp.PropertyType)
                                {
                                    //the converted value should definitely match the target prop type.
                                    targetValue = _convertedval;
                                }

                            }

                            //If we still don't get the target value, check if the property directly matches.
                            if (targetValue == null && sourceProp.PropertyType == targetProp.PropertyType)
                            {
                                targetValue = sourcePropValue;
                            }
                        }

                        if (targetValue != null)
                        {
                            break; //Cos, we managed to find a match.
                        }
                    }

                    //If we are not able to find a match, we just ignore it.
                    if (targetValue != null)
                    {
                        targetProp.SetValue(target, targetValue);
                    }
                }
                return target;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Converts DataRow to Target Object
        /// </summary>
        /// <typeparam name="TTarget"></typeparam>
        /// <param name="source">DataSource.</param>
        /// <param name="typeParser"></param>
        /// <returns></returns>
        public static TTarget Map<TTarget>(DataRow source, CustomTypeConverter typeParser = null) where TTarget : class, new() //Should be a class and also should have a parameter less new constructor.
        {

            var dataCols = source.Table.Columns.Cast<DataColumn>().Select(p => p.ColumnName)?.ToList(); //All the column names of the datarow.
            var targetProps = (typeof(TTarget))
                .GetProperties()?
                .Where(p =>
                !Attribute.IsDefined(p, typeof(IgnoreMappingAttribute)))?
                .ToList(); //This gives properties which are not ignored.

            TTarget _target = new TTarget();
            foreach (var prop in targetProps)
            {
                Map(source, prop, _target, dataCols, typeParser); //Sending datacols to save processing time.
            }

            return _target;
        }
        public static IEnumerable<TTarget> Map<TTarget>(DataTable source, CustomTypeConverter typeParser = null) where TTarget : class, new()
        {
            var dataCols = source.Columns.Cast<DataColumn>().Select(p => p.ColumnName)?.ToList(); //All the column names of the datarow.
            var targetProps = (typeof(TTarget))
                .GetProperties()?
                .Where(p =>
                !Attribute.IsDefined(p, typeof(IgnoreMappingAttribute)))?
                .ToList(); //This gives properties which are not ignored.

            List<TTarget> _targets = new List<TTarget>();
            foreach (DataRow row in source.Rows)
            {
                TTarget _target = new TTarget();
                foreach (var prop in targetProps)
                {
                    Map(row, prop, _target, dataCols, typeParser); //Sending datacols to save processing time.
                }
                _targets.Add(_target);
            }

            return _targets;
        }


        public static void Map(DataRow source, PropertyInfo prop, object target, List<string> sourceColumnNames = null, CustomTypeConverter typeParser = null)
        {
            var possibleNameMatches = _getOtherNames(prop);
            if (sourceColumnNames == null)
            {
                sourceColumnNames = source.Table.Columns.Cast<DataColumn>().Select(p => p.ColumnName)?.ToList(); //All the column names of the datarow.
            }
            //Now we need to find out if the datarow has any property with either the original prop name or the alternative name. If it is found and it matches, we get that value.

            foreach (var _name in possibleNameMatches)
            {
                if (!String.IsNullOrWhiteSpace(_name)
                    && sourceColumnNames.Any(p => p.Equals(_name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    //if a match is found.
                    var sourceValue = source[_name];
                    if (sourceValue != DBNull.Value && sourceValue != null)
                    {
                        _fillProp(prop, target, sourceValue, typeParser);
                        break;
                    }
                }
            }
        }
        private static List<string> _getOtherNames(PropertyInfo prop)
        {
            var possibleNameMatches = new List<string>() { prop.Name }; //Add default property name.
            var _otherNamesAttribute = prop.GetCustomAttribute<OtherNamesAttribute>();

            if (_otherNamesAttribute != null)
            {
                //It means that the target property has other names attribute defined and it might hold some values.
                possibleNameMatches.AddRange(_otherNamesAttribute.AlternativeNames);
            }
            return possibleNameMatches;
        }

        private static void _fillProp(PropertyInfo prop, object target, object source_value, CustomTypeConverter typeParser = null)
        {
            try
            {
                Type _propType = prop.PropertyType;
                //Intercept using type parser.
                if (typeParser != null && typeParser.Invoke(prop, source_value, out object converted_value))
                {
                    if (converted_value != null && converted_value.GetType() == prop.PropertyType)
                    {
                        prop.SetValue(target, converted_value, null);
                        return;
                    }
                }

                //STRING
                if (_propType == typeof(string))
                {
                    prop.SetValue(target, source_value.ToString().Trim(), null);
                }
                //BOOL
                else if (_propType == typeof(bool) || _propType == typeof(bool?))
                {
                    if (source_value == null)
                    {
                        prop.SetValue(source_value, null, null);
                    }
                    else
                    {
                        bool? boolval = null;

                        if (bool.TryParse(source_value.ToString(), out bool local_val))
                        {
                            //If successfully converted.
                            boolval = local_val;
                        }
                        if (boolval == null)
                        {
                            switch (source_value.ToString().ToLower())
                            {
                                case "1":
                                case "true":
                                case "okay":
                                case "success":
                                case "y":
                                case "t":
                                case "yes":
                                    boolval = true;
                                    break;
                                case "0":
                                case "false":
                                case "notokay":
                                case "fail":
                                case "n":
                                case "f":
                                case "no":
                                    boolval = false;
                                    break;
                            }
                        }

                        if (boolval != null)
                        {
                            prop.SetValue(target, boolval, null);
                        }
                    }
                }
                //INT
                else if (_propType == typeof(int)
                         || _propType == typeof(int?))
                {
                    if (source_value == null)
                    {
                        prop.SetValue(source_value, null, null);
                    }
                    else
                    {
                        int.TryParse(source_value.ToString(), out int _int_value);
                        prop.SetValue(target, _int_value, null);
                    }
                }
                //DOUBLE
                else if (_propType == typeof(double) || _propType == typeof(double?))
                {
                    if (source_value == null)
                    {
                        prop.SetValue(source_value, null, null);
                    }
                    else
                    {
                        double.TryParse(source_value.ToString(), out double dbl_value);
                        prop.SetValue(target, dbl_value, null);
                    }
                }
                //LONG
                else if (_propType == typeof(long) || _propType == typeof(long?))
                {
                    if (source_value == null)
                    {
                        prop.SetValue(source_value, null, null);
                    }
                    else
                    {
                        long.TryParse(source_value.ToString(), out long lng_value);
                        prop.SetValue(target, lng_value, null);
                    }
                }
                //DEFAULT
                else
                {
                    //TRY TO SET AS IT IS. IF IT RESULTS IN EXCEPTION, DO NOTHING/IGNORE.
                    prop.SetValue(target, source_value, null);
                }
            }
            catch (Exception)
            {
                //dO NOTHING. Use a logger to log at a later stage.
            }
        }
    }
}
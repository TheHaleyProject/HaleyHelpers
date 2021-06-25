using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Haley.Models;
using System.Reflection;

namespace Haley.Utils
{
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
        public static TTarget MapProperties<TSource,TTarget>(TSource source, TTarget target, bool include_ignored_properties = false) 
            where TSource : class  
            where TTarget : class
            {
            try
            {
                //Only take properties which are not readonly.
                var sourceProperties = source.GetType().GetProperties().Where(p => p.CanWrite);
                var targetProperties = target.GetType().GetProperties().Where(p => p.CanWrite);

                if (sourceProperties == null || sourceProperties?.Count() == 0 || targetProperties == null || targetProperties?.Count() == 0) return null;

                //Remove ignored properties
                if (!include_ignored_properties)
                {
                    List<PropertyInfo> _toremoveSource = new List<PropertyInfo>();
                    List<PropertyInfo> _toremoveTarget = new List<PropertyInfo>();

                    //Filter source. (Using Linq increases process time. So going with foreach)
                    foreach (var prop in sourceProperties.Where(p=> !Attribute.IsDefined(p,typeof(IgnoreMappingAttribute))))
                    {
                        var _mode = prop.GetCustomAttribute<IgnoreMappingAttribute>().Mode;
                        if (_mode == IgnoreMappingMode.Both || _mode == IgnoreMappingMode.FromThisObject)
                        {
                            _toremoveSource.Add(prop); //Only if both or from this object is ignored.
                        }
                    }

                    //Filter target
                    foreach (var prop in targetProperties.Where(p => !Attribute.IsDefined(p, typeof(IgnoreMappingAttribute))))
                    {
                        var _mode = prop.GetCustomAttribute<IgnoreMappingAttribute>().Mode;
                        if (_mode == IgnoreMappingMode.Both || _mode == IgnoreMappingMode.ToThisObject)
                        {
                            _toremoveTarget.Add(prop); //Only if both or from this object is ignored.
                        }
                    }


                    //Remove properties in source which has "Both" & "FromThis" ignore properties
                    sourceProperties = sourceProperties.Except(_toremoveSource);
                    targetProperties = targetProperties.Except(_toremoveTarget);
                }

                foreach (var targetProp in targetProperties)
                {
                    object sourcePropValue = null;
                    foreach (var sourceProp in sourceProperties)
                    {
                        if (sourceProp.Name == targetProp.Name && sourceProp.PropertyType == targetProp.PropertyType) //Name and type should match in both properties.
                        {
                            sourcePropValue = sourceProp.GetValue(source);
                            break;
                        }
                    }

                    //If we are not able to find a match, we just ignore it.
                    if (sourcePropValue != null)
                    {
                        targetProp.SetValue(target, sourcePropValue);
                    }
                }
                return target;
            }
            catch (Exception)
            {
                return null;
            }
            }
    }
}
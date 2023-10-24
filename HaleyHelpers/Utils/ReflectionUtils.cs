using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Data;
using Haley.Internal;
using Haley.Enums;

namespace Haley.Utils
{
    public static class ReflectionUtils {

        public static Task<object> InvokeMethod(this object input, string method_name, Type argType, Type returnType, object argument, StringComparison nameComparison = StringComparison.OrdinalIgnoreCase, string method_name_explicit= null) {
            try {

                if (input == null) throw new ArgumentNullException("input");
                //How do we call the below method asynchronously??
                var inputType = input.GetType();
                MethodInfo matchingMethod = null;
                object response = null;
                var allMethods = inputType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                // the method may be explicitly implemented (using interfaces) or direclty implemented.
                
                if (argType == null) {
                    //no input arguments required.
                    matchingMethod = inputType.GetMethods().Single(p =>
                    p.Name.Equals(method_name, nameComparison) &&
                    p.GetParameters().Length == 0 && 
                    returnType ==  p.ReturnType); //Should be a parameter less method.
                    response = matchingMethod.Invoke(input, null);
                } else {
                    matchingMethod = inputType.GetMethods().Single(p =>
                   p.Name.Equals(method_name, nameComparison) &&
                   p.GetParameters().Length == 1 &&
                   p.GetParameters().First().ParameterType == argType &&
                    returnType == p.ReturnType); //todo: Just check and confirm
                   response = matchingMethod.Invoke(input, new object[] { argument });
                }
               
                return Task.FromResult(response);
            } catch (Exception) {
                throw;
            }
        }
            public static Task<T> InvokeMethod<T>(this object input, string method_name, Type argType, object argument, StringComparison nameComparison = StringComparison.OrdinalIgnoreCase) {
                    var resultType = typeof(T); //Should we validate?
            try {
                var objectResponse = input.InvokeMethod(method_name, argType,resultType, argument, nameComparison); //Result type is important
                var response = objectResponse.Result;
                if (response != null && response.GetType().BaseType == typeof(Task)) {
                    return (Task<T>)response;
                }
                return Task.FromResult((T)response);
            } catch (Exception) {
                if (resultType.IsValueType) {
                    return Task.FromResult(default(T));
                } else {
                    return Task.FromResult(default(T));
                }
            }
        }
    }
}
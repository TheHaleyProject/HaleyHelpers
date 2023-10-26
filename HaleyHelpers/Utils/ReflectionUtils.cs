using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Data;
using Haley.Internal;
using Haley.Enums;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Runtime.InteropServices;

namespace Haley.Utils
{
    public static class ReflectionUtils {

        public static Task<MethodInfo> GetMethodInfo(Type targetType, string methodName, string interfaceExplicitName = null, Type argsType = null) {
            try {
                MethodInfo methodInfo = null;
                Func<MethodInfo, bool> methodFilter = (mI) => {
                    try {
                        // PASS THROUGH DIFFERENT FILTERS
                        //Check if argument matches
                        if (argsType != null) {
                            if (mI.GetParameters()?.Length != 1) return false;
                            if (mI.GetParameters().FirstOrDefault().ParameterType != argsType) return false;
                        }

                        return true;
                    } catch (Exception ex) {
                        return false;
                    }
                };

                if (targetType == null || string.IsNullOrWhiteSpace(methodName)) return null;

                //A specific class might implement an interface in explicit way as well. In such cases, it is difficult to identify the exact method. It is a possibility that different interfaces might have same method signature. So, we might end up calling wrong method. It is always better to check for the explicit interfacename and method availability first.

                if (interfaceExplicitName != null) {
                    var matchingNonPublic = targetType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Where(p => p.Name.Equals($@"{interfaceExplicitName}.{methodName}"));
                    matchingNonPublic = matchingNonPublic?.Where(p => methodFilter(p));
                    if (matchingNonPublic != null && matchingNonPublic.Count() == 1) {
                        methodInfo = matchingNonPublic.First();
                    }
                }

                if (methodInfo == null) {
                    //Now, check the public methods.
                    var matchingPublic = targetType.GetMethods().Where(p => p.Name.Equals(methodName)); ; //By default, it will return public instance methods.
                    matchingPublic = matchingPublic?.Where(p => methodFilter(p));
                    if (matchingPublic != null && matchingPublic.Count() == 1) {
                        methodInfo = matchingPublic.First();
                    }
                }
                return Task.FromResult(methodInfo);
            } catch (Exception ex) {
                throw;
            }

        }

        public static async Task<object> InvokeMethod(this object input, string method_name, Type argType, object argument, string interface_explicitName = null, StringComparison nameComparison = StringComparison.OrdinalIgnoreCase) {
            try {

                if (input == null) throw new ArgumentNullException("input");
                if (input is MethodInfo) throw new ArgumentException("Cannot invoke method directly on MethodInfo object type.");
                //How do we call the below method asynchronously??
                var inputType = input.GetType();
                MethodInfo matchingMethod = await GetMethodInfo(input.GetType(),method_name,interface_explicitName,argType);
                return await InvokeMethod(input, matchingMethod, argument);
            } catch (Exception ex) {
                throw;
            }
        }
        
         public static async Task<T> InvokeMethod<T>(this object input, string method_name,  Type argType = null, object argument = null, string interface_explicitName = null, StringComparison nameComparison = StringComparison.OrdinalIgnoreCase) {
            try {
                var response = await input.InvokeMethod(method_name, argType,argument,interface_explicitName, nameComparison); //Result type is important
                return await GenerateReturnParam<T>(response, method_name);
            } catch (Exception ex) {
                throw;
            }
        }

        public static async Task<object> InvokeMethod(this object input, MethodInfo method, object argument = null) {
            try {

                if (input == null) throw new ArgumentNullException("input");
                //How do we call the below method asynchronously??
                object response = null;
                if (method.GetParameters().Length == 0) {
                    response = method.Invoke(input, null);
                } else if (method.GetParameters().Length == 1) {
                    response = method.Invoke(input, new object[] { argument });
                }

                //Remember for void methods, response will be null

                if (response != null && response.GetType().BaseType == typeof(Task)) {
                    var  task = response as Task;
                    if (task == null) return response;
                    await task; // await this to be completed.
                    return task.GetType().GetProperty("Result")?.GetValue(task);
                }
                return response;
            } catch (Exception ex) {
                throw;
            }
        }

        public static async Task<T> InvokeMethod<T>(this object input, MethodInfo method, object argument = null) {
            if (input is MethodInfo) throw new ArgumentException("Cannot invoke method directly on MethodInfo object type.");
            try {
                var response = await input.InvokeMethod(method, argument); //Result type is important
                if (response == null) return default(T);
                return await GenerateReturnParam<T>(response, method.Name);
            } catch (Exception ex) {
                throw;
            }
        }

        static async Task<T> GenerateReturnParam<T>(object response, string method_name) {
            if (response != null && response.GetType().BaseType == typeof(Task)) {
                var task = response as Task<T>;
                if (task == null) throw new ArgumentException($@"Unable to convert the returned object of type {response.GetType()} to {typeof(T)} from the method {method_name} ");
                return await task;
            }

            if (response.GetType() == typeof(T)) return (T)response;
            return default(T);
        }
    }
}
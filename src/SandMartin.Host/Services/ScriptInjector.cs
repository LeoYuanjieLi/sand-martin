using System;
using Grasshopper.Kernel;
using Rhino;

namespace SandMartin.Host.Services
{
    internal static class ScriptInjector
    {
        public static bool SetComponentCode(IGH_DocumentObject obj, string code)
        {
            if (obj == null) return false;
            
            // Clean up escaped newlines from JSON
            string sanitizedCode = code?.Replace("\\n", "\n") ?? string.Empty;
            
            Log($"Attempting to set code for node {obj.InstanceGuid} ({obj.GetType().Name})");
            
            try
            {
                if (TryRhino8Injection(obj, sanitizedCode)) return true;
                if (TryDirectPropertyInjection(obj, sanitizedCode)) return true;
                if (TryLegacyInjection(obj, sanitizedCode)) return true;

                Log($"Failed to find a suitable code property for {obj.GetType().Name}.");
                return false;
            }
            catch (Exception ex)
            {
                Log($"Error in SetComponentCode: {ex.Message}");
                return false;
            }
        }

        private static bool TryRhino8Injection(IGH_DocumentObject obj, string code)
        {
            var type = obj.GetType();
            var contextField = type.GetField("Context", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (contextField == null) return false;

            var contextObj = contextField.GetValue(obj);
            if (contextObj == null) return false;

            Log("Detected Rhino 8 Script context. Injecting code...");
            bool textSet = false;

            // Step 1: Set the text
            var ctxSetTextMethod = contextObj.GetType().GetMethod("SetText", new Type[] { typeof(string) });
            if (ctxSetTextMethod != null)
            {
                ctxSetTextMethod.Invoke(contextObj, new object[] { code });
                textSet = true;
                Log("Code set via Context.SetText()");
            }
            
            if (!textSet)
            {
                var scriptProp = contextObj.GetType().GetProperty("Script");
                var scriptObj = scriptProp?.GetValue(contextObj);
                if (scriptObj != null)
                {
                    var scriptSetTextMethod = scriptObj.GetType().GetMethod("SetText", new Type[] { typeof(string) });
                    if (scriptSetTextMethod != null)
                    {
                        scriptSetTextMethod.Invoke(scriptObj, new object[] { code });
                        textSet = true;
                        Log("Code set via Script.SetText()");
                    }
                    else
                    {
                        var textProp = scriptObj.GetType().GetProperty("Text");
                        if (textProp != null)
                        {
                            textProp.SetValue(scriptObj, code);
                            textSet = true;
                            Log("Code set via Script.Text property");
                        }
                    }

                    if (textSet)
                    {
                        InvokeOptionalMethod(scriptObj, "TryBuild");
                    }
                }
            }

            // Step 2: Rebuild / Recompile if text was set
            if (textSet)
            {
                Log("Triggering rebuild sequence for Context...");
                InvokeOptionalMethod(contextObj, "TryBuildCode");
                InvokeOptionalMethod(contextObj, "OnScriptChanged");
                InvokeOptionalMethod(contextObj, "ExpireCache");
                InvokeOptionalMethod(contextObj, "ReBuild");
                
                obj.ExpireSolution(true);
                return true;
            }

            return false;
        }

        private static bool TryDirectPropertyInjection(IGH_DocumentObject obj, string code)
        {
            var type = obj.GetType();
            var mainCodeProp = type.GetProperty("Code");
            
            if (mainCodeProp != null && mainCodeProp.PropertyType == typeof(string))
            {
                mainCodeProp.SetValue(obj, code);
                Log($"Code set via direct Code property on {type.Name}.");
                obj.ExpireSolution(true);
                return true;
            }

            return false;
        }

        private static bool TryLegacyInjection(IGH_DocumentObject obj, string code)
        {
            var type = obj.GetType();
            var scriptSourceProp = type.GetProperty("ScriptSource");
            var scriptSource = scriptSourceProp?.GetValue(obj);
            
            if (scriptSource != null)
            {
                var scriptCodeProp = scriptSource.GetType().GetProperty("ScriptCode");
                if (scriptCodeProp != null)
                {
                    scriptCodeProp.SetValue(scriptSource, code);
                    Log($"Code set via legacy ScriptSource.ScriptCode on {type.Name}.");
                    obj.ExpireSolution(true);
                    return true;
                }
            }

            return false;
        }

        private static void InvokeOptionalMethod(object target, string methodName)
        {
            try
            {
                var method = target.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new Type[] { }, null);
                if (method != null)
                {
                    method.Invoke(target, null);
                    Log($"Successfully invoked {methodName}() on {target.GetType().Name}");
                }
            }
            catch (Exception ex)
            {
                Log($"Failed to invoke optional method {methodName} on {target.GetType().Name}: {ex.Message}");
            }
        }

        private static void Log(string message)
        {
            try
            {
                // Attempt to write to Rhino Command Line. 
                // This will fail with an exception if not running inside Rhino.
                RhinoApp.WriteLine($"[SandMartin] {message}");
            }
            catch
            {
                // Fallback to Console for non-Rhino environments (like unit tests)
                Console.WriteLine($"[SandMartin] {message}");
            }
        }
    }
}

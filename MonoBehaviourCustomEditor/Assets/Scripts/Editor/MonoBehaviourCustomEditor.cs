using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;
using System;

/// <summary>
/// The data structured for saving necessary informations for drawing a unknown-type field in inspector
/// </summary>
public class InspectedField
{
    internal string Label;
    internal object Value;
    internal bool IsInspected;
    internal Type FieldType;

    /// <summary>
    /// Default Constructor, no specific functionality
    /// </summary>
    internal InspectedField()
    {
        Label = "";
        Value = new object();
        IsInspected = false;
        FieldType = null;
    }

    /// <summary>
    /// Constructor, construct a Inspected field with parameters passed in.
    /// </summary>
    /// <param name="label"></param>
    /// <param name="value"></param>
    /// <param name="isInspected"></param>
    /// <param name="fieldType"></param>
    internal InspectedField(string label, object value, bool isInspected, Type fieldType)
    {
        Label = label;
        Value = value;
        IsInspected = isInspected;
        FieldType = fieldType;
    }
}

/// <summary>
/// The data structured for saving informations for drawing a function in inspector
/// </summary>
public class InspectedFunction
{
    public const int MAXPARAMETERSCOUNT = 15;
    internal string MethodName;
    internal bool IsInspected;
    private bool m_HasParameters;
    internal List<InspectedField> InspectedParameters;

    /// <summary>
    /// If the method has parameters
    /// </summary>
    internal bool HasParameters
    {
        get { return m_HasParameters; }
        set
        {
            m_HasParameters = value;
            if (value)
            {
                if (InspectedParameters == null)
                    InspectedParameters = new List<InspectedField>(MAXPARAMETERSCOUNT);
            } else
            {
                InspectedParameters = null;
            }
        }
    }

    /// <summary>
    /// Default Constructor, no specific functionality
    /// </summary>
    internal InspectedFunction()
    {
        MethodName = "";
        IsInspected = false;
        HasParameters = false;
    }

    /// <summary>
    /// Constructor, construct a InspectedMethod with parameters passed in.
    /// </summary>
    /// <param name="methodName"></param>
    /// <param name="isInspected"></param>
    /// <param name="hasParameters"></param>
    internal InspectedFunction(string methodName, bool isInspected, bool hasParameters)
    {
        MethodName = methodName;
        IsInspected = isInspected;
        HasParameters = hasParameters;
    }
}

/// <summary>
/// A Editor targeting all MonoBehaviour, which enable usage of custom attributes
/// </summary>
[CanEditMultipleObjects] // This CustomEditor is for multiple objects
[CustomEditor(typeof(MonoBehaviour), true)] // Targetting all MonoBehaviours
public class MonoBehaviourCustomEditor : Editor
{
    /// <summary>
    /// The list store all InspectedMethods info
    /// </summary>
    List<InspectedFunction> InspectedMethods = new List<InspectedFunction>();

    /// <summary>
    /// Delegate for 
    /// </summary>
    /// <param name="inspectedParameter"></param>
    /// <param name="label"></param>
    /// <param name="valueType"></param>
    /// <param name="oldValue"></param>
    /// <param name="fieldList"></param>
    /// <returns></returns>
    public delegate object DrawParameter(InspectedField inspectedParameter, string label, Type valueType, object oldValue, List<InspectedField> fieldList);

    /// <summary>
    /// Type and the type drawer method map
    /// </summary>
    readonly Dictionary<Type, DrawParameter> DrawerMap = new Dictionary<Type, DrawParameter>
    {
        {typeof(bool), DrawBool},
        {typeof(int), DrawInt},
        {typeof(long), DrawLong},
        {typeof(float), DrawFloat},
        {typeof(double), DrawDouble},
        {typeof(string), DrawString},
        {typeof(Vector2), DrawVector2},
        {typeof(Vector3), DrawVector3},
        {typeof(Vector4), DrawVector4},
        {typeof(Color), DrawColor},
        {typeof(Bounds), DrawBounds},
        {typeof(Rect), DrawRect},
    };

    public override void OnInspectorGUI()
    {
        //maintain functions of the default inspector
        DrawDefaultInspector();

        EditorGUILayout.Space(); EditorGUILayout.Space();
        var style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
        };
        EditorGUILayout.LabelField(" ---- InspectedFunctions ---- ", style, GUILayout.ExpandWidth(true));

        // Get the class type which our custom editor describe
        var type = target.GetType();

        //Reset all InspectedMethod and InspectedField status to not inspected
        foreach (var inspectedMethod in InspectedMethods)
        {
            inspectedMethod.IsInspected = false;
            if (inspectedMethod.InspectedParameters != null)
            {
                foreach (var para in inspectedMethod.InspectedParameters)
                {
                    para.IsInspected = false;
                }
            }
        }

        // Iterate over each private or public instance method (no static methods atm)
        foreach (var methodInfo in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
        {
            // try to get our custom attributes
            var attributes = methodInfo.GetCustomAttributes(typeof(InspectFunctionAttribute), true);
            if (attributes.Length > 0)
            {
                InspectedFunction inspectedMethod = null;
                foreach (var method in InspectedMethods)
                {
                    if (method.MethodName.Equals(methodInfo.Name))
                    {
                        inspectedMethod = method;
                        inspectedMethod.IsInspected = true;
                    }
                }
                if (inspectedMethod == null)
                {
                    inspectedMethod = new InspectedFunction(methodInfo.Name, true, false);
                    InspectedMethods.Add(inspectedMethod);
                }

                var passingParameter = new List<object>();

                EditorGUILayout.BeginVertical(GUI.skin.box);

                bool clicked = GUILayout.Button(methodInfo.Name);
                var parameters = methodInfo.GetParameters();
                if (parameters.Length > 0)
                {
                    inspectedMethod.HasParameters = true;
                    foreach (var para in parameters)
                    {
                        System.Object obj;
                        if (DrawField(inspectedMethod.InspectedParameters, para.ParameterType, para.Name, out obj))
                        {
                            passingParameter.Add(obj);
                        }
                        else
                        {
                            Debug.LogFormat("DrawField {0} Failed!!", para.Name);
                        }
                    }
                } else
                {
                    inspectedMethod.HasParameters = false;
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();

                if (clicked)
                {
                    methodInfo.Invoke(target, passingParameter.ToArray());
                }
            }
        }

        //Remove all InspectedMethod that is not currently inspecting
        for (int i = 0; i < InspectedMethods.Count; i++)
        {
            if (!InspectedMethods[i].IsInspected)
            {
                InspectedMethods.RemoveAt(i);
                i--;
            }
        }

        //Remove all InspectedField that is not currently inspecting
        foreach (var inspectedMethod in InspectedMethods)
        {
            if (inspectedMethod.HasParameters)
            {
                List<InspectedField> inspectedParameters = inspectedMethod.InspectedParameters;
                for (int i = 0; i < inspectedMethod.InspectedParameters.Count; i++)
                {
                    if (!inspectedParameters[i].IsInspected)
                    {
                        inspectedParameters.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        /*
        if (GUILayout.Button("CheckList"))
        {
            Debug.LogFormat("List has {0} Methods", InspectedMethods.Count);
            foreach (var method in InspectedMethods)
            {
                Debug.LogFormat("---- {0} ----", method.MethodName);
                if (method.HasParameters)
                {
                    Debug.LogFormat("Has {0} Parameters", method.InspectedParameters.Count);
                    foreach (var para in method.InspectedParameters)
                    {
                        Debug.Log(para.Label);
                    }
                }
            }
        }*/
    }

    /// <summary>
    /// Method for drawing a inspected field with type and label, stored in fieldList and return the value return by inspector UIs
    /// </summary>
    /// <param name="fieldList"></param>
    /// <param name="valueType"></param>
    /// <param name="label"></param>
    /// <param name="returnValue"></param>
    /// <returns></returns>
    public bool DrawField(List<InspectedField> fieldList, Type valueType, string label, out object returnValue)
    {
        bool isDrawn = true;
        object oldValue = new object();

        //Check if we have been inspecting the parameter already
        InspectedField inspectedParameter = null;
        foreach (var para in fieldList)
        {
            if (para.Label.Equals(label) && para.FieldType == valueType)
            {
                para.IsInspected = true;
                inspectedParameter = para;
                oldValue = para.Value;
                break;
            }
        }

        DrawParameter drawer;
        var hasTypeDrawer = DrawerMap.TryGetValue(valueType, out drawer);
        if (hasTypeDrawer)
        {
            returnValue = drawer.Invoke(inspectedParameter, label, valueType, oldValue, fieldList);
        }
        else if (typeof(UnityEngine.Object).IsAssignableFrom(valueType))
        {
            returnValue = DrawObject(inspectedParameter, label, valueType, oldValue, fieldList);
        }
        else if (typeof(UnityEngine.Component).IsAssignableFrom(valueType))
        {
            returnValue = DrawComponent(inspectedParameter, label, valueType, oldValue, fieldList);
        }
        else if (valueType.BaseType == typeof(Enum))
        {
            returnValue = DrawEnum(inspectedParameter, label, valueType, oldValue, fieldList);
        }
        else
        {
            isDrawn = false;
            returnValue = null;
        }

        return isDrawn;
    }

    #region FIELD DRAWERS
    /// <summary>
    /// Draw boolean field in inspector
    /// </summary>
    /// <param name="inspectedParameter"></param>
    /// <param name="label"></param>
    /// <param name="valueType"></param>
    /// <param name="oldValue"></param>
    /// <returns></returns>
    internal static object DrawBool(InspectedField inspectedParameter, string label, Type valueType, object oldValue, List<InspectedField> fieldList)
    {
        if (inspectedParameter == null) // it's a newly add parameter for inspecting
        {
            //Make a new InspectedField and add it to the list.
            InspectedField iField = new InspectedField(label, false, true, valueType);
            fieldList.Add(iField);

            return EditorGUILayout.Toggle(label, false);
        }
        else // we are inspecting the parameter
        {
            bool value = false;
            if (oldValue is bool)
            {
                value = (bool)oldValue;
                inspectedParameter.Value = EditorGUILayout.Toggle(label, value);

                //Set the value return from user input from inspector to the InspectedParameterList
                return inspectedParameter.Value;
            }
            else
            {
                Debug.LogFormat("{0} is not {1} type", label, valueType);
                return null;
            }
        }
    }

    /// <summary>
    /// Draw Integer field in inspector
    /// </summary>
    /// <param name="inspectedParameter"></param>
    /// <param name="label"></param>
    /// <param name="valueType"></param>
    /// <param name="oldValue"></param>
    /// <returns></returns>
    internal static object DrawInt(InspectedField inspectedParameter, string label, Type valueType, object oldValue, List<InspectedField> fieldList)
    {
        if (inspectedParameter == null) // it's a newly add parameter for inspecting
        {
            //Make a new InspectedField and add it to the list.
            InspectedField iField = new InspectedField(label, 0, true, valueType);
            fieldList.Add(iField);

            return EditorGUILayout.IntField(label, 0);
        }
        else
        {
            int value = 0;
            if (oldValue is int)
            {
                value = (int)oldValue;
                inspectedParameter.Value = EditorGUILayout.IntField(label, value);

                //Set the value return from user input from inspector to the InspectedParameterList
                return inspectedParameter.Value;
            }
            else
            {
                Debug.LogFormat("{0} is not {1} type", label, valueType);
                return null;
            }
        }
    }

    /// <summary>
    /// Draw Long Field in inspector
    /// </summary>
    /// <param name="inspectedParameter"></param>
    /// <param name="label"></param>
    /// <param name="valueType"></param>
    /// <param name="oldValue"></param>
    /// <returns></returns>
    internal static object DrawLong(InspectedField inspectedParameter, string label, Type valueType, object oldValue, List<InspectedField> fieldList)
    {
        if (inspectedParameter == null) // it's a newly add parameter for inspecting
        {
            //Make a new InspectedField and add it to the list.
            InspectedField iField = new InspectedField(label, (long)0, true, valueType);
            fieldList.Add(iField);

            return EditorGUILayout.LongField(label, 0);
        }
        else
        {
            long value = 0;
            if (oldValue is long)
            {
                value = (long)oldValue;
                inspectedParameter.Value = EditorGUILayout.LongField(label, (long)value);

                //Set the value return from user input from inspector to the InspectedParameterList
                return inspectedParameter.Value;
            }
            else
            {
                Debug.LogFormat("{0} is not {1} type", label, valueType);
                return null;
            }
        }
    }

    /// <summary>
    /// Draw float field in inspector
    /// </summary>
    /// <param name="inspectedParameter"></param>
    /// <param name="label"></param>
    /// <param name="valueType"></param>
    /// <param name="oldValue"></param>
    /// <returns></returns>
    internal static object DrawFloat(InspectedField inspectedParameter, string label, Type valueType, object oldValue, List<InspectedField> fieldList)
    {
        if (inspectedParameter == null) // it's a newly add parameter for inspecting
        {
            //Make a new InspectedField and add it to the list.
            InspectedField iField = new InspectedField(label, 0.0f, true, valueType);
            fieldList.Add(iField);

            return EditorGUILayout.FloatField(label, 0.0f);
        }
        else
        {
            float value = 0;
            if (oldValue is float)
            {
                value = (float)oldValue;
                inspectedParameter.Value = EditorGUILayout.FloatField(label, value);

                //Set the value return from user input from inspector to the InspectedParameterList
                return inspectedParameter.Value;
            }
            else
            {
                Debug.LogFormat("{0} is not {1} type", label, valueType);
                return null;
            }
        }
    }

    /// <summary>
    /// Draw double field in inspector
    /// </summary>
    /// <param name="inspectedParameter"></param>
    /// <param name="label"></param>
    /// <param name="valueType"></param>
    /// <param name="oldValue"></param>
    /// <returns></returns>
    internal static object DrawDouble(InspectedField inspectedParameter, string label, Type valueType, object oldValue, List<InspectedField> fieldList)
    {
        if (inspectedParameter == null) // it's a newly add parameter for inspecting
        {
            //Make a new InspectedField and add it to the list.
            InspectedField iField = new InspectedField(label, 0.0, true, valueType);
            fieldList.Add(iField);

            return EditorGUILayout.DoubleField(label, 0.0);
        }
        else
        {
            double value = 0.0;
            if (oldValue is double)
            {
                value = (double)oldValue;
                inspectedParameter.Value = EditorGUILayout.DoubleField(label, value);

                //Set the value return from user input from inspector to the InspectedParameterList
                return inspectedParameter.Value;
            }
            else
            {
                Debug.LogFormat("{0} is not {1} type", label, valueType);
                return null;
            }
        }
    }

    /// <summary>
    /// Draw string field in inspector
    /// </summary>
    /// <param name="inspectedParameter"></param>
    /// <param name="label"></param>
    /// <param name="valueType"></param>
    /// <param name="oldValue"></param>
    /// <returns></returns>
    internal static object DrawString(InspectedField inspectedParameter, string label, Type valueType, object oldValue, List<InspectedField> fieldList)
    {
        if (inspectedParameter == null) // it's a newly add parameter for inspecting
        {
            //Make a new InspectedField and add it to the list.
            InspectedField iField = new InspectedField(label, String.Empty, true, valueType);
            fieldList.Add(iField);

            return EditorGUILayout.TextField(label, String.Empty);
        }
        else
        {
            string value = string.Empty;
            if (oldValue is string)
            {
                value = (string)oldValue;
                inspectedParameter.Value = EditorGUILayout.TextField(label, value);

                //Set the value return from user input from inspector to the InspectedParameterList
                return inspectedParameter.Value;
            }
            else
            {
                Debug.LogFormat("{0} is not {1} type", label, valueType);
                return null;
            }
        }
    }

    /// <summary>
    /// Draw Vector2 field in inspector
    /// </summary>
    /// <param name="inspectedParameter"></param>
    /// <param name="label"></param>
    /// <param name="valueType"></param>
    /// <param name="oldValue"></param>
    /// <returns></returns>
    internal static object DrawVector2(InspectedField inspectedParameter, string label, Type valueType, object oldValue, List<InspectedField> fieldList)
    {
        if (inspectedParameter == null) // it's a newly add parameter for inspecting
        {
            //Make a new InspectedField and add it to the list.
            InspectedField iField = new InspectedField(label, Vector2.zero, true, valueType);
            fieldList.Add(iField);

            return EditorGUILayout.Vector2Field(label, Vector2.zero);
        }
        else
        {
            Vector2 value = Vector2.zero;
            if (oldValue is Vector2)
            {
                value = (Vector2)oldValue;
                inspectedParameter.Value = EditorGUILayout.Vector2Field(label, value);

                //Set the value return from user input from inspector to the InspectedParameterList
                return inspectedParameter.Value;
            }
            else
            {
                Debug.LogFormat("{0} is not {1} type", label, valueType);
                return null;
            }
        }
    }

    /// <summary>
    /// Draw Vector3 field in inspector
    /// </summary>
    /// <param name="inspectedParameter"></param>
    /// <param name="label"></param>
    /// <param name="valueType"></param>
    /// <param name="oldValue"></param>
    /// <returns></returns>
    internal static object DrawVector3(InspectedField inspectedParameter, string label, Type valueType, object oldValue, List<InspectedField> fieldList)
    {
        if (inspectedParameter == null) // it's a newly add parameter for inspecting
        {
            //Make a new InspectedField and add it to the list.
            InspectedField iField = new InspectedField(label, Vector3.zero, true, valueType);
            fieldList.Add(iField);

            return EditorGUILayout.Vector3Field(label, Vector3.zero);
        }
        else
        {
            Vector3 value = Vector3.zero;
            if (oldValue is Vector3)
            {
                value = (Vector3)oldValue;
                inspectedParameter.Value = EditorGUILayout.Vector3Field(label, value);

                //Set the value return from user input from inspector to the InspectedParameterList
                return inspectedParameter.Value;
            }
            else
            {
                Debug.LogFormat("{0} is not {1} type", label, valueType);
                return null;
            }
        }
    }

    /// <summary>
    /// Draw Vector4 field in inspector
    /// </summary>
    /// <param name="inspectedParameter"></param>
    /// <param name="label"></param>
    /// <param name="valueType"></param>
    /// <param name="oldValue"></param>
    /// <returns></returns>
    internal static object DrawVector4(InspectedField inspectedParameter, string label, Type valueType, object oldValue, List<InspectedField> fieldList)
    {
        if (inspectedParameter == null) // it's a newly add parameter for inspecting
        {
            //Make a new InspectedField and add it to the list.
            InspectedField iField = new InspectedField(label, Vector4.zero, true, valueType);
            fieldList.Add(iField);

            return EditorGUILayout.Vector4Field(label, Vector4.zero);
        }
        else
        {
            Vector4 value = Vector4.zero;
            if (oldValue is Vector4)
            {
                value = (Vector4)oldValue;
                inspectedParameter.Value = EditorGUILayout.Vector4Field(label, value);

                //Set the value return from user input from inspector to the InspectedParameterList
                return inspectedParameter.Value;
            }
            else
            {
                Debug.LogFormat("{0} is not {1} type", label, valueType);
                return null;
            }
        }
    }

    /// <summary>
    /// Draw Color field in inspector
    /// </summary>
    /// <param name="inspectedParameter"></param>
    /// <param name="label"></param>
    /// <param name="valueType"></param>
    /// <param name="oldValue"></param>
    /// <returns></returns>
    internal static object DrawColor(InspectedField inspectedParameter, string label, Type valueType, object oldValue, List<InspectedField> fieldList)
    {
        if (inspectedParameter == null) // it's a newly add parameter for inspecting
        {
            //Make a new InspectedField and add it to the list.
            InspectedField iField = new InspectedField(label, Color.white, true, valueType);
            fieldList.Add(iField);

            return EditorGUILayout.ColorField(label, Color.white);
        }
        else
        {
            Color value = Color.white;
            if (oldValue is Color)
            {
                value = (Color)oldValue;
                inspectedParameter.Value = EditorGUILayout.ColorField(label, value);

                //Set the value return from user input from inspector to the InspectedParameterList
                return inspectedParameter.Value;
            }
            else
            {
                Debug.LogFormat("{0} is not {1} type", label, valueType);
                return null;
            }
        }
    }

    /// <summary>
    /// Draw bounds field in inspector
    /// </summary>
    /// <param name="inspectedParameter"></param>
    /// <param name="label"></param>
    /// <param name="valueType"></param>
    /// <param name="oldValue"></param>
    /// <returns></returns>
    internal static object DrawBounds(InspectedField inspectedParameter, string label, Type valueType, object oldValue, List<InspectedField> fieldList)
    {
        Bounds bound = new Bounds();

        if (inspectedParameter == null) // it's a newly add parameter for inspecting
        {
            //Make a new InspectedField and add it to the list.
            InspectedField iField = new InspectedField(label, bound, true, valueType);
            fieldList.Add(iField);

            return EditorGUILayout.BoundsField(label, bound);
        }
        else
        {
            Bounds value = new Bounds();
            if (oldValue is Bounds)
            {
                value = (Bounds)oldValue;
                inspectedParameter.Value = EditorGUILayout.BoundsField(label, value);

                //Set the value return from user input from inspector to the InspectedParameterList
                return inspectedParameter.Value;
            }
            else
            {
                Debug.LogFormat("{0} is not {1} type", label, valueType);
                return null;
            }
        }
    }

    /// <summary>
    /// Draw rect field in inspector
    /// </summary>
    /// <param name="inspectedParameter"></param>
    /// <param name="label"></param>
    /// <param name="valueType"></param>
    /// <param name="oldValue"></param>
    /// <returns></returns>
    internal static object DrawRect(InspectedField inspectedParameter, string label, Type valueType, object oldValue, List<InspectedField> fieldList)
    {
        if (inspectedParameter == null) // it's a newly add parameter for inspecting
        {
            //Make a new InspectedField and add it to the list.
            InspectedField iField = new InspectedField(label, Rect.zero, true, valueType);
            fieldList.Add(iField);

            return EditorGUILayout.RectField(label, Rect.zero);
        }
        else
        {
            Rect value = Rect.zero;
            if (oldValue is Rect)
            {
                value = (Rect)oldValue;
                inspectedParameter.Value = EditorGUILayout.RectField(label, value);

                //Set the value return from user input from inspector to the InspectedParameterList
                return inspectedParameter.Value;
            }
            else
            {
                Debug.LogFormat("{0} is not {1} type", label, valueType);
                return null;
            }
        }
    }

    /// <summary>
    /// Draw an object field in inspector
    /// </summary>
    /// <param name="inspectedParameter"></param>
    /// <param name="label"></param>
    /// <param name="valueType"></param>
    /// <param name="oldValue"></param>
    /// <returns></returns>
    internal static object DrawObject(InspectedField inspectedParameter, string label, Type valueType, object oldValue, List<InspectedField> fieldList)
    {
        UnityEngine.Object obj = new UnityEngine.Object();

        if (inspectedParameter == null) // it's a newly add parameter for inspecting
        {
            //Make a new InspectedField and add it to the list.
            InspectedField iField = new InspectedField(label, obj, true, valueType);
            fieldList.Add(iField);

            return EditorGUILayout.ObjectField(label, obj, valueType, true);
        }
        else
        {
            if (oldValue is UnityEngine.Object)
            {
                obj = (UnityEngine.Object)oldValue;
                inspectedParameter.Value = EditorGUILayout.ObjectField(label, obj, valueType, true);

                //Set the value return from user input from inspector to the InspectedParameterList
                return inspectedParameter.Value;
            }
            else
            {
                Debug.LogFormat("{0} is not {1} type", label, valueType);
                return null;
            }
        }
    }

    /// <summary>
    /// Draw a component field in inspector
    /// </summary>
    /// <param name="inspectedParameter"></param>
    /// <param name="label"></param>
    /// <param name="valueType"></param>
    /// <param name="oldValue"></param>
    /// <param name="fieldList"></param>
    /// <returns></returns>
    internal static object DrawComponent(InspectedField inspectedParameter, string label, Type valueType, object oldValue, List<InspectedField> fieldList)
    {
        UnityEngine.Component component = new UnityEngine.Component();

        if (inspectedParameter == null) // it's a newly add parameter for inspecting
        {
            //Make a new InspectedField and add it to the list.
            InspectedField iField = new InspectedField(label, component, true, valueType);
            fieldList.Add(iField);

            return EditorGUILayout.ObjectField(label, component, valueType, true);
        }
        else
        {
            if (oldValue is UnityEngine.Component)
            {
                component = (UnityEngine.Component)oldValue;
                inspectedParameter.Value = EditorGUILayout.ObjectField(label, component, valueType, true);

                //Set the value return from user input from inspector to the InspectedParameterList
                return inspectedParameter.Value;
            }
            else
            {
                Debug.LogFormat("{0} is not {1} type", label, valueType);
                return null;
            }
        }
    }

    /// <summary>
    /// Draw an enum field in inspector
    /// </summary>
    /// <param name="inspectedParameter"></param>
    /// <param name="label"></param>
    /// <param name="valueType"></param>
    /// <param name="oldValue"></param>
    /// <returns></returns>
    internal static object DrawEnum(InspectedField inspectedParameter, string label, Type valueType, object oldValue, List<InspectedField> fieldList)
    {
        if (inspectedParameter == null) // it's a newly add parameter for inspecting
        {
            //Make a new InspectedField and add it to the list.
            InspectedField iField = new InspectedField(label, (Enum)Enum.GetValues(valueType).GetValue(0), true, valueType);
            fieldList.Add(iField);

            return EditorGUILayout.EnumFlagsField((Enum)Enum.GetValues(valueType).GetValue(0));
        }
        else
        {
            Enum value = (Enum)Enum.GetValues(valueType).GetValue(0);
            if (oldValue is Enum)
            {
                value = (Enum)oldValue;
                inspectedParameter.Value = EditorGUILayout.EnumPopup(value);

                //Set the value return from user input from inspector to the InspectedParameterList
                return inspectedParameter.Value;
            }
            else
            {
                Debug.LogFormat("{0} is not {1} type", label, valueType);
                return null;
            }
        }
    }
    #endregion
}

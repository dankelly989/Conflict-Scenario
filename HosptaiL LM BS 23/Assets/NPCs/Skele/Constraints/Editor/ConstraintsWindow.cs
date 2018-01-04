using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;

namespace MH.Constraints
{
    public class ConstraintsWindow : EditorWindow
    {
        private List<Type> m_allConstraintTypes = new List<Type>();
        private Vector2 m_scrollPos = Vector2.zero;

        private ConstraintStack m_cstack;

        public void SetConstraintStack(ConstraintStack cstack)
        {
            m_cstack = cstack;
        }

        void OnLostFocus()
        {
            Close();
        }

        void OnEnable()
        {
            Type tpBase = typeof(MH.Constraints.BaseConstraint);
            foreach (Type type in
                Assembly.GetAssembly(tpBase).GetTypes()
                .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(tpBase)))
            {
                m_allConstraintTypes.Add(type);
            }

            m_allConstraintTypes.Sort(CompType);
        }

        void OnGUI()
        {
            GUILayout.Label("Select a Constraint to Add...");

            m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);
            {
                foreach (var tp in m_allConstraintTypes)
                {
                    if (GUILayout.Button(tp.Name, EditorStyles.toolbarButton))
                    {
                        m_cstack.gameObject.AddComponent(tp);
                        Close();
                        break;
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private int CompType(Type lhs, Type rhs)
        {
            return lhs.FullName.CompareTo(rhs.FullName);
        }
    }
}

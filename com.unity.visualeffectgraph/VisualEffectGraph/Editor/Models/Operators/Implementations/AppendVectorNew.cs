using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Misc")]
    class AppendVectorNew : VFXOperatorNumericCascadedUnifiedNew
    {
        public override sealed string name { get { return "AppendVectorNew"; } }

        protected override sealed double defaultValueDouble { get { return 0.0f; } }

        protected override sealed bool allowInteger { get { return false; } }

        protected override Type GetExpectedOutputTypeOfOperation(IEnumerable<Type> inputTypes)
        {
            var outputComponentCount = inputTypes.Select(o => VFXExpression.TypeToSize(VFXExpression.GetVFXValueTypeFromType(o))).Sum();
            outputComponentCount = Mathf.Min(Mathf.Max(outputComponentCount, 1), 4);
            switch (outputComponentCount)
            {
                case 2: return typeof(Vector2);
                case 3: return typeof(Vector3);
                case 4: return typeof(Vector4);
                default: return typeof(float);
            }
        }

        public override void UpdateOutputExpressions()
        {
            //< Basic implementation without unified/cascaded behavior
            var inputExpressions = GetInputExpressions();
            var outputExpressions = BuildExpression(inputExpressions.ToArray());
            SetOutputExpressions(outputExpressions);
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var allComponent = inputExpression.SelectMany(e => VFXOperatorUtility.ExtractComponents(e))
                .Take(4)
                .ToArray();

            if (allComponent.Length == 0)
            {
                return new VFXExpression[] { };
            }
            else if (allComponent.Length == 1)
            {
                return allComponent;
            }
            return new[] { new VFXExpressionCombine(allComponent) };
        }
    }
}

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderGraph
{
    class PropertyCollector
    {
        public struct TextureInfo
        {
            public string name;
            public int textureId;
            public bool modifiable;
        }

        public readonly List<AbstractShaderProperty> properties = new List<AbstractShaderProperty>();

        public void AddShaderProperty(AbstractShaderProperty chunk)
        {
            if (properties.Any(x => x.referenceName == chunk.referenceName))
                return;
            properties.Add(chunk);
        }

        private const string s_UnityPerMaterialCbName = "UnityPerMaterial";

        public void GetPropertiesDeclaration(ShaderStringBuilder builder, GenerationMode mode, ConcretePrecision inheritedPrecision)
        {
            foreach (var prop in properties)
            {
                prop.ValidateConcretePrecision(inheritedPrecision);
            }

            // SamplerState properties are tricky:
            // - Unity only allows declaring SamplerState variable name of either sampler_{textureName} ("texture sampler") or SamplerState_{filterMode}_{wrapMode} ("system sampler").
            //   * That's why before the branch sg-texture-properties we have the referenceName of a SamplerStateShaderProperty set to the actual system sampler names.
            // - But with the existance of SubGraph functions we'll need unique SamplerState variable name for the function inputs.
            //   * That means if we have two SamplerState properties on the SubGraph blackboard of the same filterMode & wrapMode settings, it fails to compile because there are two
            //     identical function parameter names.
            // - So we'll have to use different names for each SamplerState property, which contradicts #1 (we could do special casing only for SubGraph function generation, but it needs
            //   changes to PropertyNode code generation, doable but more hacky).
            // - Instead, the branch sg-texture-properties changes the SamplerState property declaration to simply be:
            //       #define SamplerState_{referenceName} SamplerState{system sampler name}
            //   for all system sampler names (texture sampler names stay the same).
            //   And at the end collect all unique system sampler names and generate:
            //       SAMPLER(SamplerState{system sampler name});
            var systemSamplerNames = new HashSet<string>();

            var cbDecls = new Dictionary<string, ShaderStringBuilder>();
            foreach (var prop in properties)
            {
                var cbName = prop.propertyType.IsBatchable() ? s_UnityPerMaterialCbName : string.Empty;

                //
                // Old behaviours that I don't know why we do them:

                // If the property is not exposed, put it to Global
                if (cbName == s_UnityPerMaterialCbName && !prop.generatePropertyBlock)
                    cbName = string.Empty;
                // If we are in preview, put all CB variables to UnityPerMaterial CB
                if (cbName != string.Empty && mode == GenerationMode.Preview)
                    cbName = s_UnityPerMaterialCbName;

                if (!cbDecls.TryGetValue(cbName, out var sb))
                {
                    sb = new ShaderStringBuilder();
                    cbDecls.Add(cbName, sb);
                }

                if (prop is GradientShaderProperty gradientProperty)
                    sb.AppendLine(gradientProperty.GetGraidentPropertyDeclarationString());
                else if (prop is SamplerStateShaderProperty samplerProperty)
                    sb.AppendLine(samplerProperty.GetSamplerPropertyDeclarationString(systemSamplerNames));
                else
                    sb.AppendLine($"{prop.propertyType.FormatDeclarationString(prop.concretePrecision, prop.referenceName)};");
            }

            if (systemSamplerNames.Count > 0)
            {
                var cbName = string.Empty;
                if (!cbDecls.TryGetValue(cbName, out var sb))
                {
                    sb = new ShaderStringBuilder();
                    cbDecls.Add(cbName, sb);
                }

                SamplerStateShaderProperty.GenerateSystemSamplerNames(sb, systemSamplerNames);
            }

            foreach (var kvp in cbDecls)
            {
                if (kvp.Key != string.Empty)
                {
                    builder.AppendLine($"CBUFFER_START({kvp.Key})");
                    builder.IncreaseIndent();
                }
                builder.AppendLines(kvp.Value.ToString());
                if (kvp.Key != string.Empty)
                {
                    builder.DecreaseIndent();
                    builder.AppendLine($"CBUFFER_END");
                }
            }
            builder.AppendNewLine();
        }

        public List<TextureInfo> GetConfiguredTexutres()
        {
            var result = new List<TextureInfo>();

            foreach (var prop in properties.OfType<TextureShaderProperty>())
            {
                if (prop.referenceName != null)
                {
                    var textureInfo = new TextureInfo
                    {
                        name = prop.referenceName,
                        textureId = prop.value.texture != null ? prop.value.texture.GetInstanceID() : 0,
                        modifiable = prop.modifiable
                    };
                    result.Add(textureInfo);
                }
            }

            foreach (var prop in properties.OfType<Texture2DArrayShaderProperty>())
            {
                if (prop.referenceName != null)
                {
                    var textureInfo = new TextureInfo
                    {
                        name = prop.referenceName,
                        textureId = prop.value.textureArray != null ? prop.value.textureArray.GetInstanceID() : 0,
                        modifiable = prop.modifiable
                    };
                    result.Add(textureInfo);
                }
            }

            foreach (var prop in properties.OfType<Texture3DShaderProperty>())
            {
                if (prop.referenceName != null)
                {
                    var textureInfo = new TextureInfo
                    {
                        name = prop.referenceName,
                        textureId = prop.value.texture != null ? prop.value.texture.GetInstanceID() : 0,
                        modifiable = prop.modifiable
                    };
                    result.Add(textureInfo);
                }
            }

            foreach (var prop in properties.OfType<CubemapShaderProperty>())
            {
                if (prop.referenceName != null)
                {
                    var textureInfo = new TextureInfo
                    {
                        name = prop.referenceName,
                        textureId = prop.value.cubemap != null ? prop.value.cubemap.GetInstanceID() : 0,
                        modifiable = prop.modifiable
                    };
                    result.Add(textureInfo);
                }
            }
            return result;
        }
    }
}

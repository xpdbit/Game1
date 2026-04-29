#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace Game1.Events.Effect
{
    /// <summary>
    /// 统一效果解析器。
    /// 支持两种格式自动检测：
    /// 1. 旧版字符串编码格式 (如 "gold:+20", "hp:-15")
    /// 2. 新版结构化 XML 属性格式 (如 &lt;Effect type="Gold" operator="Add" value="20"/&gt;)
    /// 同时提供 EventTree 选择属性转换功能。
    /// </summary>
    public static class EffectParser
    {
        /// <summary>
        /// 从 XML 节点解析效果列表。自动检测格式。
        /// </summary>
        /// <param name="parentNode">包含 Effect 子节点的父节点。</param>
        /// <param name="xpath">效果子节点的 XPath，默认 "effects/Effect"。</param>
        /// <returns>解析出的 UnifiedEffect 列表。</returns>
        public static List<UnifiedEffect> ParseEffects(XmlNode parentNode, string xpath = "effects/Effect")
        {
            var results = new List<UnifiedEffect>();
            if (parentNode == null)
                return results;

            var effectNodes = parentNode.SelectNodes(xpath);
            if (effectNodes == null || effectNodes.Count == 0)
                return results;

            foreach (XmlNode node in effectNodes)
            {
                var effect = ParseSingleEffect(node);
                if (effect != null)
                    results.Add(effect);
            }

            return results;
        }

        /// <summary>
        /// 解析单个 Effect 节点。自动检测格式。
        /// </summary>
        public static UnifiedEffect? ParseSingleEffect(XmlNode node)
        {
            if (node == null)
                return null;

            // 检测格式：如果有 type 属性则为结构化格式，否则检查是否为旧版字符串格式
            if (node.Attributes?["type"]?.Value != null)
                return ParseStructuredEffect(node);
            
            // 尝试作为旧版字符串格式解析（节点内容如 "gold:+20"）
            var innerText = node.InnerText?.Trim();
            if (!string.IsNullOrEmpty(innerText))
                return ParseLegacyString(innerText);

            return null;
        }

        /// <summary>
        /// 解析新版结构化 XML 属性格式。
        /// </summary>
        public static UnifiedEffect ParseStructuredEffect(XmlNode node)
        {
            var effect = new UnifiedEffect();

            // type 属性（必需）
            var typeAttr = node.Attributes?["type"]?.Value;
            if (!string.IsNullOrEmpty(typeAttr) && Enum.TryParse<EffectType>(typeAttr, ignoreCase: true, out var type))
                effect.Type = type;

            // category 属性（可选，默认 Reward）
            if (node.Attributes?["category"] != null &&
                Enum.TryParse<EffectCategory>(node.Attributes["category"].Value, ignoreCase: true, out var category))
                effect.Category = category;

            // operator 属性（可选，默认 Add）
            if (node.Attributes?["operator"] != null &&
                Enum.TryParse<EffectOperator>(node.Attributes["operator"].Value, ignoreCase: true, out var op))
                effect.Operator = op;

            // value 属性
            if (node.Attributes?["value"] != null &&
                float.TryParse(node.Attributes["value"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                effect.Value = value;

            // randomMin / randomMax 属性（可选，用于随机区间）
            if (node.Attributes?["randomMin"] != null &&
                float.TryParse(node.Attributes["randomMin"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var randomMin))
                effect.RandomMin = randomMin;
            if (node.Attributes?["randomMax"] != null &&
                float.TryParse(node.Attributes["randomMax"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var randomMax))
                effect.RandomMax = randomMax;

            // targetId 属性（可选，用于物品ID、标志名等）
            effect.TargetId = node.Attributes?["targetId"]?.Value;

            // quantity 属性（可选，默认1）
            if (node.Attributes?["quantity"] != null &&
                int.TryParse(node.Attributes["quantity"].Value, out var quantity))
                effect.Quantity = quantity;

            // scalingStat / scalingFactor 属性（可选，用于属性缩放）
            effect.ScalingStat = node.Attributes?["scalingStat"]?.Value;
            if (node.Attributes?["scalingFactor"] != null &&
                float.TryParse(node.Attributes["scalingFactor"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var scalingFactor))
                effect.ScalingFactor = scalingFactor;

            return effect;
        }

        /// <summary>
        /// 解析旧版字符串编码格式。
        /// 支持格式：
        ///   "gold:+20"          → Gold, Add, 20
        ///   "hp:-15"            → HP, Add, -15
        ///   "exp:50"            → EXP, Add, 50
        ///   "item:ItemID:2"     → Item, TargetId=ItemID, Quantity=2
        ///   "flag:set:flagName" → Flag, Set, TargetId=flagName
        /// </summary>
        public static UnifiedEffect ParseLegacyString(string input)
        {
            var effect = new UnifiedEffect();

            if (string.IsNullOrEmpty(input))
                return effect;

            var parts = input.Split(':', 3); // 限制最多3段，防止ItemID中的冒号干扰

            // 解析效果类型
            var typeStr = parts[0].Trim().ToLowerInvariant();
            effect.Type = MapLegacyTypeString(typeStr);

            if (parts.Length == 2)
            {
                // 格式: "type:value" 如 "gold:20"、"hp:-15"
                var valueStr = parts[1].Trim();
                if (effect.Type == EffectType.Item)
                {
                    // "item:ItemID" → 只有物品ID，数量默认为1
                    effect.TargetId = valueStr;
                    effect.Quantity = 1;
                }
                else
                {
                    if (float.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
                    {
                        effect.Value = parsedValue;
                        // 负值自动推断为 Subtract 操作
                        if (parsedValue < 0 && effect.IsNumericOperation)
                            effect.Operator = EffectOperator.Subtract;
                    }
                }
            }
            else if (parts.Length >= 3)
            {
                // 格式: "type:op:value" 或 "item:ItemID:quantity"
                if (effect.Type == EffectType.Item)
                {
                    effect.TargetId = parts[1].Trim();
                    if (int.TryParse(parts[2].Trim(), out var parsedQty))
                        effect.Quantity = parsedQty;
                }
                else if (effect.Type == EffectType.Flag && parts[1].Trim().ToLowerInvariant() == "set")
                {
                    effect.Operator = EffectOperator.Set;
                    effect.TargetId = parts[2].Trim();
                }
                else
                {
                    // 尝试解析为 type:targetId:value
                    effect.TargetId = parts[1].Trim();
                    if (float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
                        effect.Value = parsedValue;
                }
            }

            // 自动推断 Category
            effect.Category = InferCategory(effect);

            return effect;
        }

        /// <summary>
        /// 将 EventTree 选择的平铺属性转换为 UnifiedEffect 列表。
        /// 处理: goldCost, setFlag, addModuleIds 等。
        /// </summary>
        /// <param name="choiceNode">EventTree Choice XML 节点。</param>
        /// <returns>转换后的 UnifiedEffect 列表。</returns>
        public static List<UnifiedEffect> ParseEventTreeChoiceEffects(XmlNode choiceNode)
        {
            var effects = new List<UnifiedEffect>();

            if (choiceNode == null)
                return effects;

            // 1. goldCost → Cost::Gold::Subtract
            var goldCostAttr = choiceNode.Attributes?["goldCost"];
            if (goldCostAttr != null && float.TryParse(goldCostAttr.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var goldCost))
            {
                effects.Add(new UnifiedEffect
                {
                    Category = EffectCategory.Cost,
                    Type = EffectType.Gold,
                    Operator = EffectOperator.Subtract,
                    Value = goldCost,
                });
            }

            // 2. setFlag → State::Flag::Set
            var setFlagAttr = choiceNode.Attributes?["setFlag"];
            if (setFlagAttr != null && !string.IsNullOrEmpty(setFlagAttr.Value))
            {
                effects.Add(new UnifiedEffect
                {
                    Category = EffectCategory.State,
                    Type = EffectType.Flag,
                    Operator = EffectOperator.Set,
                    TargetId = setFlagAttr.Value,
                });
            }

            // 3. addModuleIds → State::Module 列表
            var moduleNodes = choiceNode.SelectNodes("addModuleIds/moduleId");
            if (moduleNodes != null)
            {
                foreach (XmlNode moduleNode in moduleNodes)
                {
                    var moduleId = moduleNode.InnerText?.Trim();
                    if (!string.IsNullOrEmpty(moduleId))
                    {
                        effects.Add(new UnifiedEffect
                        {
                            Category = EffectCategory.State,
                            Type = EffectType.Module,
                            Operator = EffectOperator.Add,
                            TargetId = moduleId,
                            Quantity = 1,
                        });
                    }
                }
            }

            // 4. 嵌套 effects/Effect（已有标准格式）
            var nestedEffects = ParseEffects(choiceNode, "effects/Effect");
            effects.AddRange(nestedEffects);

            return effects;
        }

        /// <summary>
        /// 从字符串编码列表解析批量效果。
        /// </summary>
        public static List<UnifiedEffect> ParseLegacyStrings(IEnumerable<string> inputs)
        {
            var results = new List<UnifiedEffect>();
            if (inputs == null)
                return results;

            foreach (var input in inputs)
            {
                var effect = ParseLegacyString(input);
                results.Add(effect);
            }

            return results;
        }

        #region Internal Helpers

        private static EffectType MapLegacyTypeString(string typeStr)
        {
            return typeStr switch
            {
                "gold" => EffectType.Gold,
                "hp" or "health" => EffectType.HP,
                "exp" or "experience" or "xp" => EffectType.EXP,
                "item" => EffectType.Item,
                "flag" => EffectType.Flag,
                "module" => EffectType.Module,
                "combat" => EffectType.Combat,
                "damage" => EffectType.Damage,
                "heal" => EffectType.Heal,
                "buff" => EffectType.Buff,
                "unlock" => EffectType.Unlock,
                _ => EffectType.Gold, // 默认
            };
        }

        private static EffectCategory InferCategory(UnifiedEffect effect)
        {
            // 根据操作符和值推断类别
            if (effect.Operator == EffectOperator.Set && effect.Type == EffectType.Flag)
                return EffectCategory.State;

            if (effect.Type == EffectType.Module)
                return EffectCategory.State;

            if (effect.Type == EffectType.Flag)
                return EffectCategory.State;

            // 如果是负值操作，可能是消耗
            if (effect.Value < 0 && effect.Operator == EffectOperator.Add)
                return EffectCategory.Cost;

            if (effect.Operator == EffectOperator.Subtract)
                return EffectCategory.Cost;

            // 默认奖励
            return EffectCategory.Reward;
        }

        #endregion
    }
}

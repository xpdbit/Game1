using System.Collections.Generic;
using System.Xml;
using NUnit.Framework;
using Game1.Events.Effect;

namespace Game1.Tests.EditMode.Effect
{
    /// <summary>
    /// EffectParser 单元测试
    /// 测试统一效果解析器的 XML 结构化解析、旧版字符串解析、EventTree 选择效果转换
    /// </summary>
    public class EffectParserTests
    {
        [Test]
        public void StructuredXml_ParsesGoldCorrectly()
        {
            // Arrange
            var xml = "<Effect type=\"Gold\" operator=\"Add\" value=\"100\"/>";
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var node = doc.DocumentElement!;

            // Act
            var effect = EffectParser.ParseSingleEffect(node);

            // Assert
            Assert.IsNotNull(effect);
            Assert.AreEqual(EffectType.Gold, effect.Type);
            Assert.AreEqual(EffectOperator.Add, effect.Operator);
            Assert.AreEqual(100f, effect.Value);
        }

        [Test]
        public void StructuredXml_ParsesAllAttributes()
        {
            // Arrange
            var xml = "<Effect type=\"Item\" category=\"Reward\" operator=\"Add\" value=\"5\" " +
                      "targetId=\"HealthPotion\" quantity=\"3\" scalingStat=\"Attack\" scalingFactor=\"1.5\"/>";
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var node = doc.DocumentElement!;

            // Act
            var effect = EffectParser.ParseSingleEffect(node);

            // Assert
            Assert.IsNotNull(effect);
            Assert.AreEqual(EffectType.Item, effect.Type);
            Assert.AreEqual(EffectCategory.Reward, effect.Category);
            Assert.AreEqual(EffectOperator.Add, effect.Operator);
            Assert.AreEqual(5f, effect.Value);
            Assert.AreEqual("HealthPotion", effect.TargetId);
            Assert.AreEqual(3, effect.Quantity);
            Assert.AreEqual("Attack", effect.ScalingStat);
            Assert.AreEqual(1.5f, effect.ScalingFactor);
        }

        [Test]
        public void LegacyString_Gold_Add()
        {
            // Act
            var effect = EffectParser.ParseLegacyString("gold:+20");

            // Assert
            Assert.AreEqual(EffectType.Gold, effect.Type);
            Assert.AreEqual(EffectOperator.Add, effect.Operator);
            Assert.AreEqual(20f, effect.Value);
        }

        [Test]
        public void LegacyString_HP_Subtract()
        {
            // Act
            var effect = EffectParser.ParseLegacyString("hp:-15");

            // Assert
            Assert.AreEqual(EffectType.HP, effect.Type);
            Assert.AreEqual(-15f, effect.Value);
            Assert.AreEqual(EffectOperator.Subtract, effect.Operator);
        }

        [Test]
        public void LegacyString_Item_WithQuantity()
        {
            // Act
            var effect = EffectParser.ParseLegacyString("item:HealthPotion:2");

            // Assert
            Assert.AreEqual(EffectType.Item, effect.Type);
            Assert.AreEqual("HealthPotion", effect.TargetId);
            Assert.AreEqual(2, effect.Quantity);
        }

        [Test]
        public void EventTreeChoiceEffects_ParsesGoldCost()
        {
            // Arrange
            var xml = "<Choice goldCost=\"50\"/>";
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var node = doc.DocumentElement!;

            // Act
            var effects = EffectParser.ParseEventTreeChoiceEffects(node);

            // Assert
            Assert.AreEqual(1, effects.Count);
            Assert.AreEqual(EffectCategory.Cost, effects[0].Category);
            Assert.AreEqual(EffectType.Gold, effects[0].Type);
            Assert.AreEqual(EffectOperator.Subtract, effects[0].Operator);
            Assert.AreEqual(50f, effects[0].Value);
        }

        [Test]
        public void InvalidInput_ReturnsEmpty()
        {
            // Act
            var effect = EffectParser.ParseLegacyString("");

            // Assert - should return default effect (Gold, Add, 0)
            Assert.AreEqual(EffectType.Gold, effect.Type);
            Assert.AreEqual(0f, effect.Value);
        }

        [Test]
        public void NullNode_ReturnsEmptyList()
        {
            // Act
            var effects = EffectParser.ParseEffects(null);

            // Assert
            Assert.IsNotNull(effects);
            Assert.AreEqual(0, effects.Count);
        }
    }
}
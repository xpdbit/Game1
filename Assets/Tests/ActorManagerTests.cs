#define UNIT_TESTS // 定义此宏以启用独立测试模式，不依赖Unity资源加载

using System;
using System.Collections.Generic;
using System.Xml;
using NUnit.Framework;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// ActorManager 单元测试
    /// </summary>
    public class ActorManagerTests
    {
        #region ActorTemplate ParseFromXml Tests

        [Test]
        public void ParseFromXml_ValidActor_ParsesCorrectly()
        {
            // 准备XML
            var xml = @"
                <Actor id='Core.Actor.Bandit'>
                    <nameTextId>Core.Actor.Bandit.NameText</nameTextId>
                    <descTextId>Core.Actor.Bandit.DescriptionText</descTextId>
                    <affiliation>Hostile</affiliation>
                    <isBoss>false</isBoss>
                    <maxHp>20</maxHp>
                    <attack>2</attack>
                    <defense>3</defense>
                    <speed>0.8</speed>
                    <goldReward>15</goldReward>
                    <expReward>5</expReward>
                    <interactionType>None</interactionType>
                </Actor>";
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var element = doc.DocumentElement;

            // 执行
            var template = ActorTemplate.ParseFromXml(element);

            // 验证
            Assert.AreEqual("Core.Actor.Bandit", template.id);
            Assert.AreEqual("Core.Actor.Bandit.NameText", template.nameTextId);
            Assert.AreEqual("Core.Actor.Bandit.DescriptionText", template.descTextId);
            Assert.AreEqual(Affiliation.Hostile, template.affiliation);
            Assert.IsFalse(template.isBoss);
            Assert.AreEqual(20, template.maxHp);
            Assert.AreEqual(2, template.attack);
            Assert.AreEqual(3, template.defense);
            Assert.AreEqual(0.8f, template.speed);
            Assert.AreEqual(15, template.goldReward);
            Assert.AreEqual(5, template.expReward);
            Assert.AreEqual(InteractionType.None, template.interactionType);
        }

        [Test]
        public void ParseFromXml_PlayerAffiliation_ParsesCorrectly()
        {
            var xml = @"
                <Actor id='Core.Actor.Player'>
                    <nameTextId>Core.Actor.Player.NameText</nameTextId>
                    <descTextId>Core.Actor.Player.DescriptionText</descTextId>
                    <affiliation>Player</affiliation>
                    <isBoss>false</isBoss>
                    <maxHp>100</maxHp>
                    <attack>10</attack>
                    <defense>5</defense>
                    <speed>1.0</speed>
                </Actor>";
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var element = doc.DocumentElement;

            var template = ActorTemplate.ParseFromXml(element);

            Assert.AreEqual(Affiliation.Player, template.affiliation);
            Assert.IsFalse(template.isBoss);
        }

        [Test]
        public void ParseFromXml_BossActor_ParsesCorrectly()
        {
            var xml = @"
                <Actor id='Core.Actor.RoadGangLeader'>
                    <nameTextId>Core.Actor.RoadGangLeader.NameText</nameTextId>
                    <affiliation>Hostile</affiliation>
                    <isBoss>true</isBoss>
                    <maxHp>100</maxHp>
                    <attack>8</attack>
                    <defense>10</defense>
                    <speed>0.7</speed>
                </Actor>";
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var element = doc.DocumentElement;

            var template = ActorTemplate.ParseFromXml(element);

            Assert.IsTrue(template.isBoss);
            Assert.AreEqual(Affiliation.Hostile, template.affiliation);
        }

        [Test]
        public void ParseFromXml_DefaultAffiliation_ReturnsNeutral()
        {
            var xml = @"<Actor id='Test'><nameTextId>Test.Name</nameTextId></Actor>";
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var element = doc.DocumentElement;

            var template = ActorTemplate.ParseFromXml(element);

            Assert.AreEqual(Affiliation.Neutral, template.affiliation);
        }

        [Test]
        public void ParseFromXml_InvalidAffiliation_ReturnsNeutral()
        {
            var xml = @"
                <Actor id='Test'>
                    <nameTextId>Test.Name</nameTextId>
                    <affiliation>InvalidAffiliation</affiliation>
                </Actor>";
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var element = doc.DocumentElement;

            var template = ActorTemplate.ParseFromXml(element);

            Assert.AreEqual(Affiliation.Neutral, template.affiliation);
        }

        [Test]
        public void ParseFromXml_DefaultIsBoss_ReturnsFalse()
        {
            var xml = @"<Actor id='Test'><nameTextId>Test.Name</nameTextId></Actor>";
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var element = doc.DocumentElement;

            var template = ActorTemplate.ParseFromXml(element);

            Assert.IsFalse(template.isBoss);
        }

        [Test]
        public void ParseFromXml_InteractionTypeTrade_ParsesCorrectly()
        {
            var xml = @"
                <Actor id='Core.Actor.Merchant'>
                    <nameTextId>Core.Actor.Merchant.NameText</nameTextId>
                    <affiliation>Neutral</affiliation>
                    <interactionType>Trade</interactionType>
                </Actor>";
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var element = doc.DocumentElement;

            var template = ActorTemplate.ParseFromXml(element);

            Assert.AreEqual(InteractionType.Trade, template.interactionType);
        }

        [Test]
        public void ParseFromXml_DefaultMaxHp_Returns20()
        {
            var xml = @"<Actor id='Test'><nameTextId>Test.Name</nameTextId></Actor>";
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var element = doc.DocumentElement;

            var template = ActorTemplate.ParseFromXml(element);

            Assert.AreEqual(20, template.maxHp);
        }

        [Test]
        public void ParseFromXml_DefaultSpeed_Returns1()
        {
            var xml = @"<Actor id='Test'><nameTextId>Test.Name</nameTextId></Actor>";
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var element = doc.DocumentElement;

            var template = ActorTemplate.ParseFromXml(element);

            Assert.AreEqual(1f, template.speed);
        }

        [Test]
        public void ParseFromXml_EmptyInteractionType_ReturnsNone()
        {
            var xml = @"
                <Actor id='Test'>
                    <nameTextId>Test.Name</nameTextId>
                    <interactionType></interactionType>
                </Actor>";
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var element = doc.DocumentElement;

            var template = ActorTemplate.ParseFromXml(element);

            Assert.AreEqual(InteractionType.None, template.interactionType);
        }

        [Test]
        public void ParseFromXml_AllAffiliations_ParsesCorrectly()
        {
            string[] affiliations = { "Player", "Friendly", "Neutral", "Hostile", "Authority" };
            Affiliation[] expected = { Affiliation.Player, Affiliation.Friendly, Affiliation.Neutral, Affiliation.Hostile, Affiliation.Authority };

            for (int i = 0; i < affiliations.Length; i++)
            {
                var xml = $@"
                    <Actor id='Test'>
                        <nameTextId>Test.Name</nameTextId>
                        <affiliation>{affiliations[i]}</affiliation>
                    </Actor>";
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                var element = doc.DocumentElement;

                var template = ActorTemplate.ParseFromXml(element);

                Assert.AreEqual(expected[i], template.affiliation, $"Failed for affiliation: {affiliations[i]}");
            }
        }

        [Test]
        public void ParseFromXml_AllInteractionTypes_ParsesCorrectly()
        {
            string[] types = { "Trade", "Story", "Combat" };
            InteractionType[] expected = { InteractionType.Trade, InteractionType.Story, InteractionType.Combat };

            for (int i = 0; i < types.Length; i++)
            {
                var xml = $@"
                    <Actor id='Test'>
                        <nameTextId>Test.Name</nameTextId>
                        <interactionType>{types[i]}</interactionType>
                    </Actor>";
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                var element = doc.DocumentElement;

                var template = ActorTemplate.ParseFromXml(element);

                Assert.AreEqual(expected[i], template.interactionType, $"Failed for type: {types[i]}");
            }
        }

        #endregion

        #region Affiliation Enum Tests

        [Test]
        public void Affiliation_AllValues_HaveExpectedCount()
        {
            // 验证Affiliation枚举值数量
            var values = Enum.GetValues(typeof(Affiliation));
            Assert.AreEqual(6, values.Length); // None, Player, Friendly, Neutral, Hostile, Authority
        }

        [Test]
        public void InteractionType_AllValues_HaveExpectedCount()
        {
            // 验证InteractionType枚举值数量
            var values = Enum.GetValues(typeof(InteractionType));
            Assert.AreEqual(4, values.Length); // None, Trade, Story, Combat
        }

        #endregion

        #region ActorTemplate Default Values Tests

        [Test]
        public void ParseFromXml_MissingOptionalFields_UsesDefaults()
        {
            var xml = @"<Actor id='Test'><nameTextId>Test.Name</nameTextId></Actor>";
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var element = doc.DocumentElement;

            var template = ActorTemplate.ParseFromXml(element);

            // 验证所有默认值
            Assert.AreEqual(Affiliation.Neutral, template.affiliation);
            Assert.IsFalse(template.isBoss);
            Assert.AreEqual(20, template.maxHp);
            Assert.AreEqual(0, template.attack);
            Assert.AreEqual(0, template.defense);
            Assert.AreEqual(1f, template.speed);
            Assert.AreEqual(0, template.goldReward);
            Assert.AreEqual(0, template.expReward);
            Assert.AreEqual(InteractionType.None, template.interactionType);
        }

        [Test]
        public void ParseFromXml_InvalidMaxHp_UsesDefault()
        {
            var xml = @"
                <Actor id='Test'>
                    <nameTextId>Test.Name</nameTextId>
                    <maxHp>invalid</maxHp>
                </Actor>";
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var element = doc.DocumentElement;

            var template = ActorTemplate.ParseFromXml(element);

            Assert.AreEqual(20, template.maxHp); // 默认值
        }

        [Test]
        public void ParseFromXml_InvalidSpeed_UsesDefault()
        {
            var xml = @"
                <Actor id='Test'>
                    <nameTextId>Test.Name</nameTextId>
                    <speed>not_a_number</speed>
                </Actor>";
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var element = doc.DocumentElement;

            var template = ActorTemplate.ParseFromXml(element);

            Assert.AreEqual(1f, template.speed); // 默认值
        }

        #endregion
    }
}

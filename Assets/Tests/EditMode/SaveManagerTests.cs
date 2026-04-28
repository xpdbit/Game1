using System.Collections.Generic;
using System.Xml;
using NUnit.Framework;
using UnityEngine;
using Game1.Modules.Combat;

namespace Game1.Tests.EditMode
{
    /// <summary>
    /// SaveManager 单元测试
    /// 测试存档数据序列化/反序列化
    /// </summary>
    public class SaveManagerTests
    {
        #region PlayerSaveData Round-Trip Tests

        [Test]
        public void PlayerSaveData_RoundTrip_AllFieldsMatch()
        {
            // Arrange: create PlayerSaveData with inventory items, team members, skills, combat data
            var original = new PlayerSaveData
            {
                timestamp = 1234567890L,
                actorId = "Core.Actor.Player",
                actorName = "TestPlayer",
                level = 10,
                gold = 999999,
                offlineAccumulatedTime = 3600.5f
            };

            // Add inventory items
            original.inventoryItems.Add(new InventorySaveData
            {
                templateId = "Core.Item.GoldCoin",
                instanceId = 1,
                amount = 100
            });
            original.inventoryItems.Add(new InventorySaveData
            {
                templateId = "Core.Item.ShortBlade",
                instanceId = 2,
                amount = 1
            });

            // Add team members
            original.teamMembers.Add(new TeamMemberSaveData
            {
                memberId = 1,
                actorId = "Core.Actor.Warrior",
                name = "Warrior1",
                level = 8,
                currentHp = 80,
                maxHp = 100,
                attack = 25,
                defense = 15,
                speed = 10.5f,
                jobType = "Warrior"
            });

            // Add skill data
            var memberSkill = new MemberSkillSaveData
            {
                memberId = 1
            };
            memberSkill.skills.Add(new SkillSaveDataLite
            {
                skillId = "Core.Skill.Slash",
                currentLevel = 5
            });
            memberSkill.skills.Add(new SkillSaveDataLite
            {
                skillId = "Core.Skill.Guard",
                currentLevel = 3
            });
            original.skillsByMemberId.Add(memberSkill);

            // Add combat data
            original.combatData = new CombatSaveData
            {
                totalBattles = 100,
                victories = 75,
                defeats = 25,
                totalDamageDealt = 50000,
                totalDamageTaken = 30000,
                totalGoldEarned = 10000
            };

            // Act: serialize then deserialize
            var xml = original.ToXml();
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var result = new PlayerSaveData();
            result.ParseFromXml(doc.DocumentElement);

            // Assert: verify all fields
            Assert.AreEqual(original.timestamp, result.timestamp);
            Assert.AreEqual(original.actorId, result.actorId);
            Assert.AreEqual(original.actorName, result.actorName);
            Assert.AreEqual(original.level, result.level);
            Assert.AreEqual(original.gold, result.gold);
            Assert.AreEqual(original.offlineAccumulatedTime, result.offlineAccumulatedTime);

            // Verify inventory
            Assert.AreEqual(2, result.inventoryItems.Count);
            Assert.AreEqual("Core.Item.GoldCoin", result.inventoryItems[0].templateId);
            Assert.AreEqual(1, result.inventoryItems[0].instanceId);
            Assert.AreEqual(100, result.inventoryItems[0].amount);
            Assert.AreEqual("Core.Item.ShortBlade", result.inventoryItems[1].templateId);

            // Verify team members
            Assert.AreEqual(1, result.teamMembers.Count);
            Assert.AreEqual(1, result.teamMembers[0].memberId);
            Assert.AreEqual("Core.Actor.Warrior", result.teamMembers[0].actorId);
            Assert.AreEqual("Warrior1", result.teamMembers[0].name);
            Assert.AreEqual(8, result.teamMembers[0].level);
            Assert.AreEqual(80, result.teamMembers[0].currentHp);
            Assert.AreEqual(100, result.teamMembers[0].maxHp);
            Assert.AreEqual(25, result.teamMembers[0].attack);
            Assert.AreEqual(15, result.teamMembers[0].defense);
            Assert.AreEqual(10.5f, result.teamMembers[0].speed);
            Assert.AreEqual("Warrior", result.teamMembers[0].jobType);

            // Verify skills
            Assert.AreEqual(1, result.skillsByMemberId.Count);
            Assert.AreEqual(1, result.skillsByMemberId[0].memberId);
            Assert.AreEqual(2, result.skillsByMemberId[0].skills.Count);
            Assert.AreEqual("Core.Skill.Slash", result.skillsByMemberId[0].skills[0].skillId);
            Assert.AreEqual(5, result.skillsByMemberId[0].skills[0].currentLevel);
            Assert.AreEqual("Core.Skill.Guard", result.skillsByMemberId[0].skills[1].skillId);
            Assert.AreEqual(3, result.skillsByMemberId[0].skills[1].currentLevel);

            // Verify combat data
            Assert.AreEqual(100, result.combatData.totalBattles);
            Assert.AreEqual(75, result.combatData.victories);
            Assert.AreEqual(25, result.combatData.defeats);
            Assert.AreEqual(50000, result.combatData.totalDamageDealt);
            Assert.AreEqual(30000, result.combatData.totalDamageTaken);
            Assert.AreEqual(10000, result.combatData.totalGoldEarned);
        }

        #endregion

        #region GameSaveData Round-Trip Tests

        [Test]
        public void GameSaveData_RoundTrip_AllFieldsMatch()
        {
            // Arrange: create full GameSaveData with player, world, eventTreeRun data
            var original = new GameSaveData
            {
                version = 2,
                timestamp = 9876543210L,
                playTime = 3600000L, // 1000 hours
                totalInputCount = 50000
            };

            // Player data
            original.player = new PlayerSaveData
            {
                actorId = "Core.Actor.Player",
                actorName = "MainPlayer",
                level = 50,
                gold = 500000,
                offlineAccumulatedTime = 7200f
            };
            original.player.inventoryItems.Add(new InventorySaveData
            {
                templateId = "Core.Item.RareGem",
                instanceId = 100,
                amount = 5
            });

            // World data
            original.world = new WorldSaveData
            {
                currentNodeIndex = 25,
                currentMapSeed = "MapSeed12345",
                travelProgress = 0.75f
            };

            // EventTreeRun data
            original.eventTreeRun = new EventTreeRunSaveData
            {
                templateId = "Core.EventTree.MainStory",
                currentNodeId = "Node_05",
                isRunning = true
            };
            original.eventTreeRun.history.Add("Node_01");
            original.eventTreeRun.history.Add("Node_02");
            original.eventTreeRun.history.Add("Node_03");

            // Act: serialize then deserialize
            var xml = original.ToXml();
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var result = new GameSaveData();
            result.ParseFromXml(doc.DocumentElement);

            // Assert: verify all fields
            Assert.AreEqual(2, result.version);
            Assert.AreEqual(9876543210L, result.timestamp);
            Assert.AreEqual(3600000L, result.playTime);
            Assert.AreEqual(50000, result.totalInputCount);

            // Verify player
            Assert.AreEqual("Core.Actor.Player", result.player.actorId);
            Assert.AreEqual("MainPlayer", result.player.actorName);
            Assert.AreEqual(50, result.player.level);
            Assert.AreEqual(500000, result.player.gold);
            Assert.AreEqual(7200f, result.player.offlineAccumulatedTime);
            Assert.AreEqual(1, result.player.inventoryItems.Count);
            Assert.AreEqual("Core.Item.RareGem", result.player.inventoryItems[0].templateId);

            // Verify world
            Assert.AreEqual(25, result.world.currentNodeIndex);
            Assert.AreEqual("MapSeed12345", result.world.currentMapSeed);
            Assert.AreEqual(0.75f, result.world.travelProgress);

            // Verify eventTreeRun
            Assert.AreEqual("Core.EventTree.MainStory", result.eventTreeRun.templateId);
            Assert.AreEqual("Node_05", result.eventTreeRun.currentNodeId);
            Assert.IsTrue(result.eventTreeRun.isRunning);
            Assert.AreEqual(3, result.eventTreeRun.history.Count);
            Assert.AreEqual("Node_01", result.eventTreeRun.history[0]);
            Assert.AreEqual("Node_02", result.eventTreeRun.history[1]);
            Assert.AreEqual("Node_03", result.eventTreeRun.history[2]);
        }

        #endregion

        #region TeamMemberSaveData XML Tag Tests

        [Test]
        public void TeamMemberSaveData_ToXml_UsesCorrectTag()
        {
            // Arrange
            var member = new TeamMemberSaveData
            {
                memberId = 1,
                actorId = "Core.Actor.Knight",
                name = "SirGalahad",
                level = 15,
                currentHp = 150,
                maxHp = 200,
                attack = 30,
                defense = 20,
                speed = 12.5f,
                jobType = "Knight"
            };

            // Act
            var xml = member.ToXml();

            // Assert: verify XML contains correct tag
            Assert.IsTrue(xml.Contains("<TeamMemberSaveData>"));
            Assert.IsTrue(xml.Contains("</TeamMemberSaveData>"));
            Assert.IsFalse(xml.Contains("<TeamMemberData>"));
        }

        [Test]
        public void TeamMemberSaveData_RoundTrip_PreservesAllFields()
        {
            // Arrange
            var original = new TeamMemberSaveData
            {
                memberId = 42,
                actorId = "Core.Actor.Mage",
                name = "Merlin",
                level = 20,
                currentHp = 80,
                maxHp = 100,
                attack = 45,
                defense = 8,
                speed = 15.0f,
                jobType = "Mage"
            };

            // Act
            var xml = original.ToXml();
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var result = new TeamMemberSaveData();
            result.ParseFromXml(doc.DocumentElement);

            // Assert
            Assert.AreEqual(42, result.memberId);
            Assert.AreEqual("Core.Actor.Mage", result.actorId);
            Assert.AreEqual("Merlin", result.name);
            Assert.AreEqual(20, result.level);
            Assert.AreEqual(80, result.currentHp);
            Assert.AreEqual(100, result.maxHp);
            Assert.AreEqual(45, result.attack);
            Assert.AreEqual(8, result.defense);
            Assert.AreEqual(15.0f, result.speed);
            Assert.AreEqual("Mage", result.jobType);
        }

        #endregion

        #region IncrementalSaveData Round-Trip Tests

        [Test]
        public void IncrementalSaveData_RoundTrip_AllFieldsMatch()
        {
            // Arrange
            var original = new IncrementalSaveData
            {
                baseTimestamp = 1000000000L,
                timestamp = 1000003600L,
                slotData = "<GameSaveData><player>test</player></GameSaveData>"
            };
            original.changedSlots.Add("Player");
            original.changedSlots.Add("Inventory");

            // Act
            var xml = original.ToXml();
            var result = IncrementalSaveData.ParseFromXmlString(xml);

            // Assert
            Assert.AreEqual(1000000000L, result.baseTimestamp);
            Assert.AreEqual(1000003600L, result.timestamp);
            Assert.AreEqual(2, result.changedSlots.Count);
            Assert.IsTrue(result.changedSlots.Contains("Player"));
            Assert.IsTrue(result.changedSlots.Contains("Inventory"));
            Assert.AreEqual(original.slotData, result.slotData);
        }

        [Test]
        public void IncrementalSaveData_RoundTrip_MultipleChangedSlots()
        {
            // Arrange
            var original = new IncrementalSaveData
            {
                baseTimestamp = 500L,
                timestamp = 1500L
            };
            original.changedSlots.Add("Player");
            original.changedSlots.Add("World");
            original.changedSlots.Add("EventTree");
            original.changedSlots.Add("PlayTime");
            original.changedSlots.Add("Combat");
            original.slotData = "SomeSlotDataContent";

            // Act
            var xml = original.ToXml();
            var result = IncrementalSaveData.ParseFromXmlString(xml);

            // Assert
            Assert.AreEqual(5, result.changedSlots.Count);
            Assert.AreEqual("Player", result.changedSlots[0]);
            Assert.AreEqual("World", result.changedSlots[1]);
            Assert.AreEqual("EventTree", result.changedSlots[2]);
            Assert.AreEqual("PlayTime", result.changedSlots[3]);
            Assert.AreEqual("Combat", result.changedSlots[4]);
            Assert.AreEqual("SomeSlotDataContent", result.slotData);
        }

        #endregion

        #region CombatSaveData Round-Trip Tests

        [Test]
        public void CombatSaveData_RoundTrip_AllFieldsMatch()
        {
            // Arrange
            var original = new CombatSaveData
            {
                totalBattles = 200,
                victories = 150,
                defeats = 50,
                totalDamageDealt = 100000,
                totalDamageTaken = 60000,
                totalGoldEarned = 25000
            };

            // Act
            var xml = original.ToXml();
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var result = new CombatSaveData();
            result.ParseFromXml(doc.DocumentElement);

            // Assert
            Assert.AreEqual(200, result.totalBattles);
            Assert.AreEqual(150, result.victories);
            Assert.AreEqual(50, result.defeats);
            Assert.AreEqual(100000, result.totalDamageDealt);
            Assert.AreEqual(60000, result.totalDamageTaken);
            Assert.AreEqual(25000, result.totalGoldEarned);
        }

        [Test]
        public void CombatSaveData_ToStatistics_AllFieldsMatch()
        {
            // Arrange
            var statistics = new CombatStatistics
            {
                totalBattles = 500,
                victories = 350,
                defeats = 150,
                totalDamageDealt = 250000,
                totalDamageTaken = 180000,
                totalGoldEarned = 50000
            };
            var combatSaveData = new CombatSaveData(statistics);

            // Act
            var result = combatSaveData.ToStatistics();

            // Assert
            Assert.AreEqual(500, result.totalBattles);
            Assert.AreEqual(350, result.victories);
            Assert.AreEqual(150, result.defeats);
            Assert.AreEqual(250000, result.totalDamageDealt);
            Assert.AreEqual(180000, result.totalDamageTaken);
            Assert.AreEqual(50000, result.totalGoldEarned);
            Assert.AreEqual(0.7f, result.winRate);
        }

        [Test]
        public void CombatSaveData_ConstructorWithNull_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                var combatSaveData = new CombatSaveData(null);
                Assert.AreEqual(0, combatSaveData.totalBattles);
            });
        }

        #endregion

        #region MemberSkillSaveData Serialization Tests

        [Test]
        public void MemberSkillSaveData_ToXml_ContainsSkillSaveDataLiteElements()
        {
            // Arrange
            var memberSkill = new MemberSkillSaveData
            {
                memberId = 5
            };
            memberSkill.skills.Add(new SkillSaveDataLite
            {
                skillId = "Core.Skill.Fireball",
                currentLevel = 10
            });
            memberSkill.skills.Add(new SkillSaveDataLite
            {
                skillId = "Core.Skill.IceShard",
                currentLevel = 7
            });

            // Act
            var xml = memberSkill.ToXml();

            // Assert
            Assert.IsTrue(xml.Contains("<MemberSkillSaveData>"));
            Assert.IsTrue(xml.Contains("<memberId>5</memberId>"));
            Assert.IsTrue(xml.Contains("<SkillSaveDataLite>"));
            Assert.IsTrue(xml.Contains("<skillId>Core.Skill.Fireball</skillId>"));
            Assert.IsTrue(xml.Contains("<currentLevel>10</currentLevel>"));
            Assert.IsTrue(xml.Contains("<skillId>Core.Skill.IceShard</skillId>"));
            Assert.IsTrue(xml.Contains("<currentLevel>7</currentLevel>"));
        }

        [Test]
        public void MemberSkillSaveData_RoundTrip_AllSkillsPreserved()
        {
            // Arrange
            var original = new MemberSkillSaveData
            {
                memberId = 99
            };
            original.skills.Add(new SkillSaveDataLite { skillId = "Skill_A", currentLevel = 1 });
            original.skills.Add(new SkillSaveDataLite { skillId = "Skill_B", currentLevel = 2 });
            original.skills.Add(new SkillSaveDataLite { skillId = "Skill_C", currentLevel = 3 });

            // Act
            var xml = original.ToXml();
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var result = new MemberSkillSaveData();
            result.ParseFromXml(doc.DocumentElement);

            // Assert
            Assert.AreEqual(99, result.memberId);
            Assert.AreEqual(3, result.skills.Count);
            Assert.AreEqual("Skill_A", result.skills[0].skillId);
            Assert.AreEqual(1, result.skills[0].currentLevel);
            Assert.AreEqual("Skill_B", result.skills[1].skillId);
            Assert.AreEqual(2, result.skills[1].currentLevel);
            Assert.AreEqual("Skill_C", result.skills[2].skillId);
            Assert.AreEqual(3, result.skills[2].currentLevel);
        }

        #endregion

        #region SaveSlot Enum Tests

        [Test]
        public void SaveSlot_FlagsCombination_WorksCorrectly()
        {
            // Act
            var playerFlag = SaveSlot.Player;
            var worldFlag = SaveSlot.World;
            var eventTreeFlag = SaveSlot.EventTree;
            var playTimeFlag = SaveSlot.PlayTime;
            var inputCountFlag = SaveSlot.InputCount;
            var allFlag = SaveSlot.All;

            // Assert individual flags
            Assert.AreEqual(1, (int)playerFlag);
            Assert.AreEqual(2, (int)worldFlag);
            Assert.AreEqual(4, (int)eventTreeFlag);
            Assert.AreEqual(8, (int)playTimeFlag);
            Assert.AreEqual(16, (int)inputCountFlag);
            Assert.AreEqual(31, (int)allFlag); // 1|2|4|8|16 = 31

            // Assert combined flags
            Assert.IsTrue(allFlag.HasFlag(SaveSlot.Player));
            Assert.IsTrue(allFlag.HasFlag(SaveSlot.World));
            Assert.IsTrue(allFlag.HasFlag(SaveSlot.EventTree));
            Assert.IsTrue(allFlag.HasFlag(SaveSlot.PlayTime));
            Assert.IsTrue(allFlag.HasFlag(SaveSlot.InputCount));
        }

        [Test]
        public void SaveSlot_AllFlags_IncludeInputCount()
        {
            // Act
            var combined = SaveSlot.Player | SaveSlot.World | SaveSlot.EventTree | SaveSlot.PlayTime | SaveSlot.InputCount;

            // Assert
            Assert.AreEqual(SaveSlot.All, combined);
        }

        [Test]
        public void SaveSlot_None_HasNoFlags()
        {
            // Assert
            Assert.AreEqual(0, (int)SaveSlot.None);
            Assert.IsFalse(SaveSlot.None.HasFlag(SaveSlot.Player));
            Assert.IsFalse(SaveSlot.None.HasFlag(SaveSlot.World));
        }

        [Test]
        public void SaveSlot_SubsetFlags_WorkCorrectly()
        {
            // Act
            var subset = SaveSlot.Player | SaveSlot.World;

            // Assert
            Assert.IsTrue(subset.HasFlag(SaveSlot.Player));
            Assert.IsTrue(subset.HasFlag(SaveSlot.World));
            Assert.IsFalse(subset.HasFlag(SaveSlot.EventTree));
            Assert.AreEqual(3, (int)subset);
        }

        #endregion

        #region GameSaveData ParseFromXmlString Tests

        [Test]
        public void GameSaveData_ParseFromXmlString_ValidXml_ReturnsCorrectData()
        {
            // Arrange: a complete XML string
            var xmlString = @"<GameSaveData>
                <version>1</version>
                <timestamp>1000</timestamp>
                <playTime>5000</playTime>
                <totalInputCount>1000</totalInputCount>
                <player>
                    <timestamp>2000</timestamp>
                    <actorId>TestActor</actorId>
                    <actorName>TestName</actorName>
                    <level>15</level>
                    <gold>5000</gold>
                    <offlineAccumulatedTime>1800</offlineAccumulatedTime>
                    <inventoryItems></inventoryItems>
                    <teamMembers></teamMembers>
                    <skillsByMemberId></skillsByMemberId>
                    <combatData>
                        <totalBattles>50</totalBattles>
                        <victories>40</victories>
                        <defeats>10</defeats>
                        <totalDamageDealt>10000</totalDamageDealt>
                        <totalDamageTaken>5000</totalDamageTaken>
                        <totalGoldEarned>2000</totalGoldEarned>
                    </combatData>
                </player>
                <world>
                    <timestamp>3000</timestamp>
                    <currentNodeIndex>10</currentNodeIndex>
                    <currentMapSeed>TestSeed</currentMapSeed>
                    <travelProgress>0.5</travelProgress>
                </world>
                <eventTreeRun>
                    <timestamp>4000</timestamp>
                    <templateId>TestEventTree</templateId>
                    <currentNodeId>Node1</currentNodeId>
                    <isRunning>true</isRunning>
                    <history></history>
                </eventTreeRun>
            </GameSaveData>";

            // Act
            var result = GameSaveData.ParseFromXmlString(xmlString);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.version);
            Assert.AreEqual(1000, result.timestamp);
            Assert.AreEqual(5000, result.playTime);
            Assert.AreEqual(1000, result.totalInputCount);

            // Player assertions
            Assert.AreEqual("TestActor", result.player.actorId);
            Assert.AreEqual("TestName", result.player.actorName);
            Assert.AreEqual(15, result.player.level);
            Assert.AreEqual(5000, result.player.gold);
            Assert.AreEqual(1800, result.player.offlineAccumulatedTime);
            Assert.AreEqual(50, result.player.combatData.totalBattles);
            Assert.AreEqual(40, result.player.combatData.victories);

            // World assertions
            Assert.AreEqual(10, result.world.currentNodeIndex);
            Assert.AreEqual("TestSeed", result.world.currentMapSeed);
            Assert.AreEqual(0.5f, result.world.travelProgress);

            // EventTreeRun assertions
            Assert.AreEqual("TestEventTree", result.eventTreeRun.templateId);
            Assert.AreEqual("Node1", result.eventTreeRun.currentNodeId);
            Assert.IsTrue(result.eventTreeRun.isRunning);
        }

        [Test]
        public void GameSaveData_ParseFromXmlString_NullOrEmpty_ReturnsNull()
        {
            // Assert
            Assert.IsNull(GameSaveData.ParseFromXmlString(null));
            Assert.IsNull(GameSaveData.ParseFromXmlString(""));
            Assert.IsNull(GameSaveData.ParseFromXmlString("   "));
        }

        [Test]
        public void GameSaveData_ParseFromXmlString_InvalidRoot_ReturnsNull()
        {
            // Arrange
            var invalidXml = "<NotGameSaveData></NotGameSaveData>";

            // Act
            var result = GameSaveData.ParseFromXmlString(invalidXml);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region Edge Cases Tests

        [Test]
        public void PlayerSaveData_RoundTrip_EmptyLists_Preserved()
        {
            // Arrange
            var original = new PlayerSaveData
            {
                actorId = "TestActor",
                actorName = "EmptyTest",
                level = 1,
                gold = 0
            };
            // Keep lists empty intentionally

            // Act
            var xml = original.ToXml();
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var result = new PlayerSaveData();
            result.ParseFromXml(doc.DocumentElement);

            // Assert
            Assert.AreEqual(0, result.inventoryItems.Count);
            Assert.AreEqual(0, result.teamMembers.Count);
            Assert.AreEqual(0, result.skillsByMemberId.Count);
        }

        [Test]
        public void PlayerSaveData_RoundTrip_NullStrings_Preserved()
        {
            // Arrange
            var original = new PlayerSaveData
            {
                actorId = null,
                actorName = null,
                level = 1,
                gold = 0
            };

            // Act
            var xml = original.ToXml();
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var result = new PlayerSaveData();
            result.ParseFromXml(doc.DocumentElement);

            // Assert - null strings become empty strings after parse
            Assert.AreEqual(string.Empty, result.actorId);
            Assert.AreEqual(string.Empty, result.actorName);
        }

        [Test]
        public void PlayerSaveData_RoundTrip_ExtremeGoldValue()
        {
            // Arrange
            var original = new PlayerSaveData
            {
                actorId = "RichGuy",
                actorName = "MoneyBags",
                level = 99,
                gold = int.MaxValue
            };

            // Act
            var xml = original.ToXml();
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var result = new PlayerSaveData();
            result.ParseFromXml(doc.DocumentElement);

            // Assert
            Assert.AreEqual(int.MaxValue, result.gold);
        }

        [Test]
        public void WorldSaveData_RoundTrip_NegativeTimestamp()
        {
            // Arrange
            var original = new WorldSaveData
            {
                timestamp = -1000L,
                currentNodeIndex = 5,
                currentMapSeed = "NegativeTest",
                travelProgress = 0.5f
            };

            // Act
            var xml = original.ToXml();
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var result = new WorldSaveData();
            result.ParseFromXml(doc.DocumentElement);

            // Assert
            Assert.AreEqual(-1000L, result.timestamp);
        }

        [Test]
        public void IncrementalSaveData_RoundTrip_EmptyChangedSlots()
        {
            // Arrange
            var original = new IncrementalSaveData
            {
                baseTimestamp = 100L,
                timestamp = 200L,
                slotData = "NoChangesData"
            };
            // Keep changedSlots empty

            // Act
            var xml = original.ToXml();
            var result = IncrementalSaveData.ParseFromXmlString(xml);

            // Assert
            Assert.AreEqual(100L, result.baseTimestamp);
            Assert.AreEqual(200L, result.timestamp);
            Assert.AreEqual(0, result.changedSlots.Count);
            Assert.AreEqual("NoChangesData", result.slotData);
        }

        [Test]
        public void EventTreeRunSaveData_RoundTrip_EmptyHistory()
        {
            // Arrange
            var original = new EventTreeRunSaveData
            {
                templateId = "TestTree",
                currentNodeId = "Start",
                isRunning = false
            };
            // History intentionally empty

            // Act
            var xml = original.ToXml();
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var result = new EventTreeRunSaveData();
            result.ParseFromXml(doc.DocumentElement);

            // Assert
            Assert.AreEqual("TestTree", result.templateId);
            Assert.AreEqual("Start", result.currentNodeId);
            Assert.IsFalse(result.isRunning);
            Assert.AreEqual(0, result.history.Count);
        }

        #endregion
    }
}
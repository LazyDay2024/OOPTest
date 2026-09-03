using NUnit.Framework;
using TowerDefense.Buffs;
using TowerDefense.Data;

namespace TowerDefense.Tests
{
    /// <summary>
    /// Pure-logic tests that need no scene: layer eligibility (the LSP fix at the
    /// type level) and buff math.
    /// </summary>
    public sealed class CoreLogicTests
    {
        [Test]
        public void GroundMask_CanTarget_Ground_ButNot_Air()
        {
            Assert.IsTrue(TargetLayer.Ground.CanTarget(EnemyLayer.Ground));
            Assert.IsFalse(TargetLayer.Ground.CanTarget(EnemyLayer.Air));
        }

        [Test]
        public void AirMask_CanTarget_Air_ButNot_Ground()
        {
            Assert.IsTrue(TargetLayer.Air.CanTarget(EnemyLayer.Air));
            Assert.IsFalse(TargetLayer.Air.CanTarget(EnemyLayer.Ground));
        }

        [Test]
        public void BothMask_CanTarget_Everything()
        {
            Assert.IsTrue(TargetLayer.Both.CanTarget(EnemyLayer.Ground));
            Assert.IsTrue(TargetLayer.Both.CanTarget(EnemyLayer.Air));
        }

        [Test]
        public void NoneMask_CanTarget_Nothing()
        {
            Assert.IsFalse(TargetLayer.None.CanTarget(EnemyLayer.Ground));
            Assert.IsFalse(TargetLayer.None.CanTarget(EnemyLayer.Air));
        }

        [Test]
        public void SlowBuff_HalvesSpeed_WhenPercentIsHalf()
        {
            var buff = new SlowBuff(0.5f, 3f);
            Assert.AreEqual(0.5f, buff.SpeedMultiplier, 1e-4f);
        }

        [Test]
        public void SlowBuff_ClampsPercent_IntoValidRange()
        {
            Assert.AreEqual(0f, new SlowBuff(5f, 1f).SpeedMultiplier, 1e-4f);   // >1 clamps to full stop
            Assert.AreEqual(1f, new SlowBuff(-2f, 1f).SpeedMultiplier, 1e-4f);  // <0 clamps to no slow
        }

        [Test]
        public void SlowBuff_Expires_AfterDurationElapses()
        {
            var buff = new SlowBuff(0.3f, 1f);
            buff.Tick(0.5f, null);
            Assert.IsFalse(buff.IsExpired);
            buff.Tick(0.6f, null);
            Assert.IsTrue(buff.IsExpired);
        }

        [Test]
        public void SpawnGroup_KeepsConstructorValues()
        {
            var group = new SpawnGroup(null, 7);
            Assert.AreEqual(7, group.Count);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using GameFramework;
using GameFramework.Fsm;
using UnityEngine;

public class Monster : TargetableObject {
    [SerializeField]
    private MonsterData m_MonsterData = null;

    /// <summary>
    /// 攻击是否正在冷却
    /// </summary>
    private bool m_IsAtkCDing = false;
    /// <summary>
    /// 状态类状态机：CD空闲、CD
    /// </summary>
    private GameFramework.Fsm.IFsm<Monster> m_MonsterStateFsm;
    /// <summary>
    /// 行动类状态机：空闲、行走、攻击、受伤
    /// </summary>
    private GameFramework.Fsm.IFsm<Monster> m_MonsterActionFsm;

    /// <summary>
    /// 是否正在追踪目标
    /// </summary>
    /// <returns></returns>
    public bool IsLockingAim { get; set; }
    /// <summary>
    /// 锁定的目标
    /// </summary>
    /// <returns></returns>
    public Entity LockingAim { get; set; }

    protected override void OnInit (object userData) {
        base.OnInit (userData);

    }

    protected override void OnShow (object userData) {
        base.OnShow (userData);

        m_MonsterData = userData as MonsterData;
        if (m_MonsterData == null) {
            Log.Error ("Monster data is invalid.");
            return;
        }

        IsLockingAim = false;

        /* 创建状态机 */
        m_MonsterStateFsm = GameEntry.Fsm.CreateFsm<Monster>("monsterStateFsm", this, new FsmState<Monster>[]{
            new MonsterCDIdleState(),
            new MonsterAtkCDState(),
        });

        m_MonsterActionFsm = GameEntry.Fsm.CreateFsm<Monster>("monsterActionFsm", this, new FsmState<Monster>[]{
            new MonsterIdleState(),
            new MonsterWalkState(),
            new MonsterAtkState(),
            new MonsterHurtState(),
        });

        /* 启动状态机 */
        m_MonsterStateFsm.Start<MonsterCDIdleState> ();
        m_MonsterActionFsm.Start<MonsterIdleState> ();
    }

    protected override void OnUpdate (float elapseSeconds, float realElapseSeconds) {
        base.OnUpdate (elapseSeconds, realElapseSeconds);
    }

    protected override void OnHide (object userData) {
        base.OnHide (userData);

        GameEntry.Fsm.DestroyFsm<Monster> ();
    }

    public override ImpactData GetImpactData () {
        return new ImpactData (m_MonsterData.Camp, m_MonsterData.HP, 0, m_MonsterData.Def);
    }

    /// <summary>
    /// 向前移动
    /// </summary>
    /// <param name="distance"></param>
    public void Forward (float distance) {
        CachedTransform.position += CachedTransform.forward * distance * m_MonsterData.MoveSpeed;
    }

    /// <summary>
    /// 转身
    /// </summary>
    /// <param name="destVec">目标位置</param>
    public void Rotate(Vector3 destVec) {
        CachedTransform.Rotate(destVec);
    }

    /// <summary>
    /// 切换动画
    /// </summary>
    /// <param name="state"></param>
    public void ChangeAnimation (MonsterAnimationState state) {
        // Log.Info("Monster ChangeAnimation：" + state);
        
        if (state == MonsterAnimationState.walk) {
            CachedAnimator.SetBool ("IsWalking", true);
            CachedAnimator.SetBool ("IsAttacking", false);
            CachedAnimator.SetBool ("IsHurting", false);
        } else if (state == MonsterAnimationState.idle) {
            CachedAnimator.SetBool ("IsWalking", false);
            CachedAnimator.SetBool ("IsAttacking", false);
            CachedAnimator.SetBool ("IsHurting", false);
        } else if (state == MonsterAnimationState.atk) {
            CachedAnimator.SetBool ("IsWalking", false);
            CachedAnimator.SetBool ("IsAttacking", true);
            CachedAnimator.SetBool ("IsHurting", false);
        } else if (state == MonsterAnimationState.hurt) {
            CachedAnimator.SetBool ("IsWalking", false);
            CachedAnimator.SetBool ("IsAttacking", false);
            CachedAnimator.SetBool ("IsHurting", true);
        }
    }

    /// <summary>
    /// 是否在攻击范围内
    /// </summary>
    /// <param name="distance"></param>
    /// <returns></returns>
    public bool CheckInAtkRange(float distance) {
        return distance <= m_MonsterData.AtkRange;
    }

    /// <summary>
    /// 是否在追踪范围内
    /// </summary>
    /// <param name="distance"></param>
    /// <returns></returns>
    public bool CheckInSeekRange(float distance) {
        return distance <= m_MonsterData.SeekRange;
    }

    /// <summary>
    /// 执行攻击
    /// </summary>
    /// <param name="aimEntity">攻击目标</param>
    public void PerformAttack(TargetableObject aimEntity) {
        m_IsAtkCDing = true;
        m_MonsterStateFsm.FireEvent(this, MonsterAttackEventArgs.EventId);
        aimEntity.ApplyDamage(m_MonsterData.Atk);
    }

    /// <summary>
    /// 接受伤害
    /// </summary>
    /// <param name="damageHP"></param>
    public override void ApplyDamage (int damageHP) {
        m_MonsterActionFsm.FireEvent (this, ApplyDamageEventArgs.EventId, damageHP);
    }

    /// <summary>
    /// 攻击是否正在冷却
    /// </summary>
    /// <returns></returns>
    public bool IsAtkCDing() {
        return m_IsAtkCDing;
    }

    /// <summary>
    /// 重置攻击冷却
    /// </summary>
    public void ResetAtkCD() {
        m_IsAtkCDing = false;
    }

    /// <summary>
    /// 锁定目标
    /// </summary>
    /// <param name="aim"></param>
    public void LockAim(Entity aim) {
        this.LockingAim = aim;
        this.IsLockingAim = true;
    }

    /// <summary>
    /// 解除目标的锁定
    /// </summary>
    public void UnlockAim() {
        this.LockingAim = null;
        this.IsLockingAim = false;
    }

    public MonsterData MonsterData {
        get {
            return m_MonsterData;
        }
    }
}
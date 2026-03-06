using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using BrilliantStone.Content.Buffs;      // 确保此命名空间包含您的 BrilliantInfection

/// <summary>
/// 可复用的史莱姆AI，包含原版所有逻辑 + 智能寻敌系统 + 自定义Buff友好化。
/// 修复了卡住时无法跳跃的问题，增加了地面检测容错。
/// </summary>
public static class SlimeAI
{
    /// <summary>
    /// 史莱姆AI的核心更新方法，应在ModNPC.AI()中调用。
    /// </summary>
    /// <param name="npc">NPC实例</param>
    /// <param name="p">配置参数</param>
    public static void UpdateAI(NPC npc, SlimeAIParameters p)
    {
        // 获取有效目标（考虑自定义友好Buff）
        Player targetPlayer = GetTargetPlayer(npc, p);
        if (targetPlayer == null)
        {
            // 没有可攻击的玩家，执行空闲行为
            IdleBehaviour(npc, p);
            // 空闲时根据速度设置方向（可能为零速度，保持原方向）
            if (npc.velocity.X != 0)
                npc.spriteDirection = Math.Sign(npc.velocity.X);
            // 空闲时强制归零旋转（避免残留倾斜）
            npc.rotation = 0f;
            return;
        }

        // 更新npc.target字段，以便其他系统（如旗帜）正确工作
        npc.target = targetPlayer.whoAmI;

        // 计算与目标的距离和方向
        float distanceX = targetPlayer.Center.X - npc.Center.X;
        float distanceY = targetPlayer.Center.Y - npc.Center.Y;
        float distance = (float)Math.Sqrt(distanceX * distanceX + distanceY * distanceY);

        // 仇恨检测：超出仇恨范围则停止追逐
        if (distance > p.aggroRange && distance > p.chaseRange)
        {
            IdleBehaviour(npc, p);
            if (npc.velocity.X != 0)
                npc.spriteDirection = Math.Sign(npc.velocity.X);
            npc.rotation = 0f;
            return;
        }

        // 视线检测（如果启用高级寻路）
        bool canSeePlayer = !p.useAdvancedPathfinding || Collision.CanHit(npc.position, npc.width, npc.height, targetPlayer.position, targetPlayer.width, targetPlayer.height);
        bool playerOnHighGround = targetPlayer.Top.Y < npc.Top.Y - 16 * 3; // 玩家高于NPC 3格以上

        // 游泳逻辑（如果在水或岩浆中）
        bool inLiquid = npc.wet;
        if (inLiquid && !p.immuneToLava && npc.lavaWet) // 如果在岩浆中且不免疫，则受到伤害
        {
            npc.life -= 5;
            npc.HitEffect();
            if (npc.life <= 0)
            {
                npc.NPCLoot();
                npc.active = false;
            }
        }

        // 地面检测（严格意义：速度Y为0且不液体中）
        bool onGround = npc.velocity.Y == 0f && !inLiquid;

        // 跳跃逻辑（使用增强的CanJumpNow检测）
        HandleJumping(npc, p, targetPlayer, onGround, inLiquid, distanceX, distance, canSeePlayer, playerOnHighGround);

        // 水平移动
        HandleMovement(npc, p, targetPlayer, distanceX, onGround, inLiquid, canSeePlayer);

        // 水中浮动（原版史莱姆在水中会缓慢上浮）
        if (inLiquid && p.canSwim)
        {
            if (npc.velocity.Y > -0.5f)
                npc.velocity.Y -= 0.02f;
        }

        // 避免掉落悬崖（如果启用）
        if (p.canJumpOverGaps && onGround && !canSeePlayer)
        {
            // 简单的悬崖检测：前方一格无物块
            Vector2 aheadPos = npc.Center + new Vector2((distanceX > 0 ? 16 : -16), 0);
            if (!WorldGen.SolidTile((int)(aheadPos.X / 16), (int)((aheadPos.Y + 16) / 16)))
            {
                // 前方是悬崖，尝试跳跃
                if (npc.velocity.Y == 0)
                {
                    npc.velocity.Y = -p.jumpSpeed;
                    npc.velocity.X = (distanceX > 0 ? p.moveSpeed : -p.moveSpeed) * 1.5f;
                }
            }
        }

        // 设置旋转：根据水平速度倾斜，速度为零时归零
        npc.rotation = npc.velocity.X * p.tiltFactor;
        // 设置贴图方向：根据移动方向翻转
        if (npc.velocity.X != 0)
            npc.spriteDirection = Math.Sign(npc.velocity.X);

        // 调用自定义行为委托（如果有）
        p.CustomAction?.Invoke(npc, p, targetPlayer);
    }

    /// <summary>
    /// 增强的地面检测：允许在轻微悬空但下方有固体时视为“可跳跃”。
    /// </summary>
    private static bool CanJumpNow(NPC npc, bool inLiquid)
    {
        if (inLiquid) return true;
        if (npc.velocity.Y == 0f) return true;

        // 如果垂直速度极小（<0.2）且正下方有固体物块，也视为可跳跃（防卡死）
        if (Math.Abs(npc.velocity.Y) < 0.2f)
        {
            int tileX = (int)(npc.Center.X / 16);
            int tileY = (int)((npc.position.Y + npc.height) / 16) + 1; // 下方一格
            if (WorldGen.SolidTile(tileX, tileY))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 根据自定义Buff（如 BrilliantInfection）获取合适的攻击目标。
    /// 返回第一个没有该Buff的最近玩家；如果所有玩家都有Buff，则返回null。
    /// </summary>
    private static Player GetTargetPlayer(NPC npc, SlimeAIParameters p)
    {
        if (!p.respectRoyalGel)
        {
            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest(true);
            Player anyPlayer = Main.player[npc.target];
            return (anyPlayer.active && !anyPlayer.dead) ? anyPlayer : null;
        }

        int closestPlayer = -1;
        float closestDistSq = float.MaxValue;
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];
            if (player.active && !player.dead)
            {
                // 检测玩家是否拥有您的模组提供的友好Buff
                if (player.HasBuff(ModContent.BuffType<BrilliantInfection>()))
                    continue;

                float distSq = Vector2.DistanceSquared(npc.Center, player.Center);
                if (distSq < closestDistSq)
                {
                    closestDistSq = distSq;
                    closestPlayer = i;
                }
            }
        }
        return closestPlayer != -1 ? Main.player[closestPlayer] : null;
    }

    private static void IdleBehaviour(NPC npc, SlimeAIParameters p)
    {
        bool inLiquid = npc.wet; // 用于CanJumpNow判断

        // 跳跃冷却（与战斗共用localAI[0]）
        int jumpCooldown = (int)npc.localAI[0];
        if (jumpCooldown > 0)
        {
            npc.localAI[0]--;
        }
        else if (CanJumpNow(npc, inLiquid) && Main.rand.NextFloat() < p.idleJumpChance)
        {
            // 执行跳跃
            npc.velocity.Y = -p.jumpSpeed * (1f + Main.rand.NextFloat(-p.idleJumpSpeedVariance, p.idleJumpSpeedVariance));
            // 跳跃时给予随机水平速度，使动作更灵动
            float randomDir = Main.rand.NextFloat(-1f, 1f);
            npc.velocity.X = randomDir * p.idleJumpHorizontalSpeed;
            // 设置方向以匹配跳跃方向
            if (Math.Abs(randomDir) > 0.1f)
                npc.spriteDirection = Math.Sign(randomDir);
            // 重置冷却
            npc.localAI[0] = Main.rand.Next(p.jumpCooldownMin, p.jumpCooldownMax + 1);
        }

        // 在地面上且几乎静止时，有几率轻微转向（模拟四处张望）
        if (npc.velocity.Y == 0f && Math.Abs(npc.velocity.X) < 0.1f && Main.rand.NextBool(300))
        {
            npc.spriteDirection *= -1;
        }
    }

    private static void HandleJumping(NPC npc, SlimeAIParameters p, Player player, bool onGround, bool inLiquid,
                                      float distanceX, float distance, bool canSeePlayer, bool playerOnHighGround)
    {
        int jumpCooldown = (int)npc.localAI[0];
        if (jumpCooldown > 0)
        {
            npc.localAI[0]--;
            return;
        }

        bool canJump = CanJumpNow(npc, inLiquid);
        if (!canJump)
            return;

        bool shouldJump = false;
        float jumpVelocity = p.jumpSpeed;

        // 条件1：玩家距离较近，直接攻击性跳跃（优先级最高）
        if (distance < 200f && Math.Abs(distanceX) > 30f)
        {
            shouldJump = true;
            jumpVelocity = p.jumpChaseSpeed;
        }
        // 条件2：前方有障碍物（一格高的固体），需要跳跃越过
        else if (onGround && Math.Abs(distanceX) > 30f) // 只有正在移动时才检测障碍
        {
            int direction = Math.Sign(distanceX);
            // 检测前方一格（水平方向）是否有固体物块
            int frontTileX = (int)((npc.Center.X + direction * 20) / 16);
            int frontTileY = (int)((npc.position.Y + npc.height) / 16); // 脚下一格
            bool frontBlocked = WorldGen.SolidTile(frontTileX, frontTileY);

            // 如果前方有固体，或者速度极小（被卡住），则跳跃
            if (frontBlocked || Math.Abs(npc.velocity.X) < 0.1f)
            {
                shouldJump = true;
                jumpVelocity = p.jumpSpeed; // 使用基础跳跃速度
            }
        }
        // 条件3：玩家在高台上（原条件）
        else if (playerOnHighGround && onGround && distance < p.chaseRange)
        {
            float heightNeeded = (player.Center.Y - npc.Center.Y) / -16f;
            if (heightNeeded > 0 && heightNeeded <= p.maxJumpHeight)
            {
                shouldJump = true;
                jumpVelocity = p.jumpSpeed + heightNeeded * 0.2f;
            }
        }
        // 条件4：水中跳出水面
        else if (inLiquid && npc.velocity.Y > -0.1f && player.Center.Y < npc.Center.Y - 100)
        {
            shouldJump = true;
            jumpVelocity = p.jumpSpeed;
        }

        if (shouldJump)
        {
            npc.velocity.Y = -jumpVelocity;
            if (distanceX != 0)
                npc.velocity.X = (distanceX > 0 ? p.moveSpeed : -p.moveSpeed) * 1.2f;
            npc.localAI[0] = Main.rand.Next(p.jumpCooldownMin, p.jumpCooldownMax + 1);
        }
    }

    private static void HandleMovement(NPC npc, SlimeAIParameters p, Player player, float distanceX,
                                       bool onGround, bool inLiquid, bool canSeePlayer)
    {
        float targetSpeed = p.moveSpeed;
        int direction = 0;

        // 始终根据玩家水平位置决定方向，不再受视线或墙体影响
        if (distanceX > 30f)
            direction = 1;
        else if (distanceX < -30f)
            direction = -1;

        // 应用水平速度
        if (direction != 0)
        {
            if (onGround || inLiquid)
                npc.velocity.X += direction * 0.1f;
            else
                npc.velocity.X += direction * 0.05f; // 空中惯性小

            // 限制最大速度
            if (Math.Abs(npc.velocity.X) > targetSpeed)
                npc.velocity.X = Math.Sign(npc.velocity.X) * targetSpeed;
        }
        else
        {
            // 减速
            if (onGround)
                npc.velocity.X *= 0.8f;
            else
                npc.velocity.X *= 0.95f;
        }
    }

    /// <summary>
    /// 处理死亡分裂，应在OnKill中调用（如果启用分裂）。
    /// </summary>
    public static void DoSplit(NPC npc, SlimeAIParameters p)
    {
        if (!p.splitOnDeath || p.splitCount <= 0 || p.splitType < 0)
            return;

        for (int i = 0; i < p.splitCount; i++)
        {
            int newNPC = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, p.splitType);
            if (newNPC < Main.maxNPCs)
            {
                NPC child = Main.npc[newNPC];
                child.velocity = new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, -1f));

                // 设置子体血量：优先使用自定义值，否则按母体最大生命值平分
                if (p.splitChildLife > 0)
                    child.life = p.splitChildLife;
                else
                    child.life = npc.lifeMax / p.splitCount;
            }
        }
    }
}

/// <summary>
/// 史莱姆AI的所有可配置参数。
/// </summary>
public class SlimeAIParameters
{
    /// <summary>跳跃基础速度（正数，越大跳越高）</summary>
    public float jumpSpeed = 4.0f;
    /// <summary>追击时的跳跃速度（通常比基础快）</summary>
    public float jumpChaseSpeed = 6.0f;
    /// <summary>水平移动速度</summary>
    public float moveSpeed = 2.0f;
    /// <summary>最大跳跃高度（格数），用于判断能否跳上高台</summary>
    public float maxJumpHeight = 10f;

    /// <summary>跳跃后的最小冷却时间（帧）</summary>
    public int jumpCooldownMin = 15;
    /// <summary>跳跃后的最大冷却时间（帧）</summary>
    public int jumpCooldownMax = 30;

    /// <summary>空闲时随机跳跃的概率（每帧）</summary>
    public float idleJumpChance = 0.005f;
    /// <summary>空闲跳跃速度的随机浮动范围（例如 0.2 表示 ±20%）</summary>
    public float idleJumpSpeedVariance = 0.2f;
    /// <summary>空闲跳跃时给予的水平速度大小，使动作更灵动（0表示完全垂直）</summary>
    public float idleJumpHorizontalSpeed = 1.0f;

    /// <summary>能否游泳（水中上浮）</summary>
    public bool canSwim = true;
    /// <summary>是否免疫岩浆（若false，在岩浆中会持续受伤）</summary>
    public bool immuneToLava = false;

    /// <summary>死亡时是否分裂</summary>
    public bool splitOnDeath = false;
    /// <summary>分裂数量</summary>
    public int splitCount = 2;
    /// <summary>分裂出的NPC类型ID</summary>
    public int splitType = NPCID.BlueSlime;
    /// <summary>分裂子体的生命值（若>0则使用此值，否则自动计算为母体最大生命/分裂数）</summary>
    public int splitChildLife = 0;

    /// <summary>是否启用高级寻路（视线检测、绕路、跳跃高台）</summary>
    public bool useAdvancedPathfinding = false;
    /// <summary>仇恨范围（像素），超出此范围停止追逐</summary>
    public float aggroRange = 400f;
    /// <summary>追逐范围（像素），在此范围内会尝试接近玩家</summary>
    public float chaseRange = 1000f;
    /// <summary>能否跳过悬崖（前方无地面时跳跃）</summary>
    public bool canJumpOverGaps = true;

    /// <summary>是否检测自定义Buff（BrilliantInfection）来决定是否攻击玩家</summary>
    public bool respectRoyalGel = true;

    /// <summary>倾斜因子：rotation = velocity.X * tiltFactor，设置为0可禁用倾斜</summary>
    public float tiltFactor = 0.1f;

    /// <summary>自定义行为委托，可在每一帧执行额外逻辑</summary>
    public Action<NPC, SlimeAIParameters, Player> CustomAction = null;
}
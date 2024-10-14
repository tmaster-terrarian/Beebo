using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Jelly;
using Jelly.Components;
using Microsoft.Xna.Framework.Input;
using System;
using Jelly.Graphics;
using Jelly.Utilities;
using Beebo.Graphics;

namespace Beebo.GameContent.Components;

public enum PlayerState
{
    IgnoreState,
    StandIdle,
    Normal,
    Dead,
    LedgeGrab,
    LedgeClimb,
    Wallslide,
}

public class Player : Actor
{
    private static readonly Random nugdeRandom = new();

    private PlayerState _state = PlayerState.Normal; // please do NOT touch this thx
    private bool _stateJustChanged;
    private int stateTimer;

    private readonly float gravity = 0.2f;
    private readonly float baseMoveSpeed = 2;
    private readonly float baseJumpSpeed = -3.7f;
    private readonly float baseGroundAcceleration = 0.10f;
    private readonly float baseGroundFriction = 0.14f;
    private readonly float baseAirAcceleration = 0.07f;
    private readonly float baseAirFriction = 0.02f;
    private readonly int baseBulletDelay = 6;
    private readonly int baseBombDelay = 90;

    private float moveSpeed;
    private float jumpSpeed;
    private float accel;
    private float fric;

    private int jumpBuffer;
    private int inputDir;
    private bool jumpCancelled;
    private bool running;
    private bool skidding;
    private bool wasOnGround;
    private bool onJumpthrough;

    private Vector2 oldVelocity;

    private float duck;

    private bool canJump = true;
    private bool canWalljump = true;
    private bool canLedgeGrab = true;

    private bool fxTrail;
    private int fxTrailCounter;
    private readonly List<AfterImage> afterImages = [];

    private readonly List<Texture2D> textures = [];
    private readonly List<int> frameCounts = [];
    private TextureIndex textureIndex;
    private float frame;

    private float squash = 1;
    private float stretch = 1;

    private float recoil;
    private int bulletDelay;
    private int bombDelay;

    // TIMERS
    private int ledgegrabTimer;
    private int landTimer;
    private int wallslideTimer;

    private float hp = 100;

    private Rectangle MaskNormal = new(-4, -14, 8, 14);
    private Rectangle MaskDuck = new(-4, -8, 8, 8);
    private Rectangle MaskLedge = new(-8, 0, 8, 14);
    private Point PivotNormal = new(12, 24);
    private Point PivotDuck = new(12, 24);
    private Point PivotLedge = new(18, 12);

    private Solid platformTarget = null;

    public float Lookup { get; private set; }

    class AfterImage
    {
        public float Alpha = 1;
        public TextureIndex TextureIndex;
        public int Frame;
        public Vector2 Position;
        public int Facing;
        public Vector2 Scale = Vector2.One;
        public Color Color = Color.White;
        public float Rotation;
        public SpriteEffects SpriteEffects = SpriteEffects.None;
        public Vector2 Pivot;
    }

    public enum TextureIndex
    {
        Idle,
        LookUp,
        Crawl,
        Duck,
        Dead,
        Jump,
        Run,
        RunFast,
        Wallslide,
        LedgeGrab,
        LedgeClimb,
    }

    public PlayerInputMapping InputMapping { get; } = new() {
        Left = new MappedInput.Keyboard(Keys.A),
        Right = new MappedInput.Keyboard(Keys.D),
        Up = new MappedInput.Keyboard(Keys.W),
        Down = new MappedInput.Keyboard(Keys.S),
    };

    public PlayerState State {
        get => _state;
        set {
            if(_state != value)
            {
                _stateJustChanged = true;
                stateTimer = 0;

                OnStateExit(_state);
                OnStateEnter(value);

                _state = value;
            }
        }
    }

    public bool FaceTowardsMouse { get; private set; } = true;

    public int VisualFacing {
        get {
            if(State != PlayerState.Normal) return Facing;

            int facing = FaceTowardsMouse ? Math.Sign(Main.Camera.MousePositionInWorld.X - Center.X) : Facing;
            if(facing == 0) facing = Facing;
            return facing;
        }
    }

    public bool UseGamePad { get; set; }

    public bool Dead { get; private set; }

    public override void OnCreated()
    {
        AddTexture("idle", 6);
        AddTexture("lookup", 6);
        AddTexture("crawl", 8);
        AddTexture("duck", 4);
        AddTexture("dead", 3);
        AddTexture("jump", 6);
        AddTexture("run", 8);
        AddTexture("run_fast", 6);
        AddTexture("wallslide");
        AddTexture("ledgegrab");
        AddTexture("ledgeclimb", 3);

        SetHitbox(MaskNormal, PivotNormal);

        Entity.Depth = 50;
        Entity.Tag.Add(EntityTags.Player);
    }

    void AddTexture(string path, int frameCount = 1)
    {
        const string texPath = "Images/Player/";
        textures.Add(Main.LoadContent<Texture2D>(texPath + path));
        frameCounts.Add(frameCount);
    }

    void SetHitbox(Rectangle mask, Point pivot)
    {
        bboxOffset = mask.Location;
        Width = mask.Width;
        Height = mask.Height;
        Entity.Pivot = pivot;
    }

    public override void Update()
    {
        wasOnGround = OnGround;
        onJumpthrough = CheckCollidingJumpthrough(BottomEdge.Shift(0, 1));
        if(onJumpthrough) OnGround = true;
        else OnGround = CheckColliding(BottomEdge.Shift(0, 1));

        if(!wasOnGround && OnGround)
        {
            Grounded();
        }

        if(textureIndex == TextureIndex.LookUp || textureIndex == TextureIndex.Idle)
            frame += 0.2f;

        running = textureIndex == TextureIndex.Run || textureIndex == TextureIndex.RunFast;

        if(State != PlayerState.LedgeClimb)
            ledgegrabTimer = MathUtil.Approach(ledgegrabTimer, 0, 1);

        inputDir = InputMapping.Right.IsDown.ToInt32() - InputMapping.Left.IsDown.ToInt32();

        if(OnGround && State != PlayerState.LedgeGrab)
        {
            platformTarget = Scene.CollisionSystem.SolidPlace(Hitbox.Shift(0, 2));
        }
        else if(Scene.CollisionSystem.SolidMeeting(Hitbox.Shift(2 * inputDir, 0)) && (State == PlayerState.Normal || State == PlayerState.Wallslide))
        {
            platformTarget = Scene.CollisionSystem.SolidPlace(Hitbox.Shift(2 * inputDir, 0));
        }
        else if(jumpBuffer < 10 && State != PlayerState.LedgeGrab)
        {
            platformTarget = null;
        }

        RecalculateStats();

        if(!OnGround)
            duck = 0;
        if(duck > 0)
        {
            moveSpeed *= 0.5f;
            if(Math.Abs(velocity.X) > moveSpeed)
                velocity.X = MathUtil.Approach(velocity.X, moveSpeed * inputDir, 0.25f);
        }

        StateUpdate();

        #region Jump Logic
        if(InputMapping.Jump.Pressed && canJump)
        {
            if(OnGround || (jumpBuffer > 0 && velocity.Y > 0))
            {
                platformTarget = null;
                // var s = null;
                if(duck <= 0)
                {
                    State = PlayerState.Normal;
                    frame = 0;
                    textureIndex = TextureIndex.Jump;
                    var c = Scene.CollisionSystem.SolidPlace(new Rectangle(Center.X, Bottom + 1, 1, 1));
                    if(c is not null)
                    {
                        oldVelocity.X = c.velocity.X;
                        oldVelocity.Y = c.velocity.Y;
                        velocity.X += c.velocity.X;
                        if(c.velocity.Y < 0)
                            velocity.Y = c.velocity.Y;
                    }
                    velocity.Y = jumpSpeed;
                    // s = _audio_play_sound(sn_jump, 0, false);
                }
                else if(!CheckColliding(Hitbox.Shift(0, -2)))
                {
                    State = PlayerState.Normal;
                    var c = Scene.CollisionSystem.SolidPlace(new Rectangle(Center.X, Bottom + 1, 1, 1));
                    if(c is not null)
                    {
                        oldVelocity.X = c.velocity.X;
                        oldVelocity.Y = c.velocity.Y;
                        velocity.X += c.velocity.X;
                        if(c.velocity.Y < 0)
                            velocity.Y = c.velocity.Y;
                    }
                    velocity.Y += jumpSpeed / 2;
                    // s = _audio_play_sound(sn_jump, 0, false);
                }
                else
                {
                    // s = _audio_play_sound(sn_jump, 0, false);
                }

                if(!OnGround)
                {
                    if(CheckColliding(Hitbox.Shift((int)moveSpeed, 0), true) && canWalljump)
                    {
                        State = PlayerState.Normal;
                        velocity.X = -moveSpeed;
                        velocity.Y = jumpSpeed;
                        Facing = -1;
                        var w = Scene.CollisionSystem.SolidPlace(Hitbox.Shift((int)moveSpeed, 0));
                        if(w is not null)
                            velocity.X += w.velocity.X / 2;
                        // audio_stop_sound(s);
                        // _audio_play_sound(sn_walljump, 0, false);
                    }
                    else if(CheckColliding(Hitbox.Shift((int)-moveSpeed, 0), true) && canWalljump)
                    {
                        State = PlayerState.Normal;
                        velocity.X = moveSpeed;
                        velocity.Y = jumpSpeed;
                        Facing = 1;
                        var w = Scene.CollisionSystem.SolidPlace(Hitbox.Shift((int)-moveSpeed, 0));
                        if(w is not null)
                            velocity.X += w.velocity.X / 2;
                        // audio_stop_sound(s);
                        // _audio_play_sound(sn_walljump, 0, false);
                    }
                    else if(jumpBuffer > 0 && velocity.Y > 0)
                    {
                        jumpBuffer = 0;
                        for (var i = 0; i < 4; i++)
                        {
                            // var p = instance_create_depth((bbox_left + random(8)), random_range(bbox_bottom, bbox_bottom), (depth - 1), fx_dust);
                            //     p.textureIndex = spr_fx_dust2;
                            //     p.vx = random_range(-0.5, 0.5);
                            //     p.vz = random_range(-0.2, 0);
                        }
                    }
                }
            }
            else if(State == PlayerState.LedgeGrab || State == PlayerState.LedgeClimb)
            {
                ledgegrabTimer = 15;
                Collides = true;
                var c = platformTarget;
                if(c is not null)
                {
                    oldVelocity.X = c.velocity.X;
                    oldVelocity.Y = c.velocity.Y;
                    velocity.X = c.velocity.X;
                    if(c.velocity.Y < 0)
                        velocity.Y = c.velocity.Y;
                }

                SetHitbox(MaskNormal, PivotNormal);
                stateTimer = 0;

                if(inputDir != 0) // if input then jump off with some horizontal speed
                {
                    velocity.X += moveSpeed * 0.8f * inputDir + (0.4f * -Facing);

                    if(!CheckColliding(Hitbox.Shift(0, c is not null ? c.Top - Entity.Y : 0), c is null)) // if theres space jump as normal
                        velocity.Y -= 2.7f * (!InputMapping.Down.IsDown).ToInt32();
                    else // else displace the player first
                    {
                        if(State == PlayerState.LedgeGrab)
                            Entity.X -= 4 * Facing;
                        Entity.Y += 12;
                        velocity.Y -= 2.7f * (!InputMapping.Down.IsDown).ToInt32();
                        textureIndex = TextureIndex.Jump;
                        frame = 0;
                    }
                    // _audio_play_sound(sn_walljump, 0, false);
                }
                else // otherwise just hop off
                {
                    if(State == PlayerState.LedgeGrab)
                        Entity.X -= 4 * Facing;
                    Entity.Y += 12;
                    velocity.Y -= 2.7f * (!InputMapping.Down.IsDown).ToInt32();
                    textureIndex = TextureIndex.Jump;
                    frame = 0;
                    // _audio_play_sound(sn_walljump, 0, false);
                }
                State = PlayerState.Normal;
            }
            else if(canWalljump)
            {
                if(CheckColliding(Hitbox.Shift((int)moveSpeed, 0), true))
                {
                    platformTarget = null;
                    State = PlayerState.Normal;
                    velocity.X = -moveSpeed;
                    velocity.Y = jumpSpeed;
                    Facing = -1;
                    var w = Scene.CollisionSystem.SolidPlace(Hitbox.Shift((int)moveSpeed, 0));
                    if(w is not null)
                        velocity.X += w.velocity.X / 2;
                    // _audio_play_sound(sn_walljump, 0, false);
                }
                else if(CheckColliding(Hitbox.Shift((int)-moveSpeed, 0), true))
                {
                    platformTarget = null;
                    State = PlayerState.Normal;
                    velocity.X = moveSpeed;
                    velocity.Y = jumpSpeed;
                    Facing = 1;
                    var w = Scene.CollisionSystem.SolidPlace(Hitbox.Shift((int)-moveSpeed, 0));
                    if(w is not null)
                        velocity.X += w.velocity.X / 2;
                    // _audio_play_sound(sn_walljump, 0, false);
                }
            }
        }

        if(InputMapping.Jump.Released && velocity.Y < 0 && !jumpCancelled)
        {
            jumpCancelled = true;
            velocity.Y /= 2;
        }
        #endregion

        if(velocity.Y > 4)
        {
            squash = MathHelper.Clamp(1.01f * (velocity.Y / 12), 1, 1.4f);
            stretch = MathHelper.Clamp(0.99f / (velocity.Y / 12), 0.65f, 1);
        }

        if(skidding && OnGround && duck == 0)
        {
            textureIndex = TextureIndex.Run;
            frame = 6;

            // with(instance_create_depth(x, bbox_bottom, (depth - 10), fx_dust))
            // {
            //     sprite_index = spr_fx_dust2;
            //     image_index = irandom(1)
            //     vx = random_range(-0.1, 0.1);
            //     vy = random_range(-0.5, -0.1);
            //     vz = 0;
            // }
        }

        if(Input.GetDown(Keys.LeftControl))
        {
            BottomMiddle = Main.Camera.MousePositionInWorld;
            velocity = Vector2.Zero;
        }

        if(!float.IsNormal(velocity.X)) velocity.X = 0;
        if(!float.IsNormal(velocity.Y)) velocity.Y = 0;

        MoveX(velocity.X, () => {
            if(State == PlayerState.Dead)
            {
                velocity.X = -velocity.X * 0.9f;
            }
            else for(int j = 0; j < MathUtil.RoundToInt(MathHelper.Max(Time.DeltaTime, 1)); j++)
            {
                if(inputDir != 0 && !CheckColliding(Hitbox.Shift(inputDir, -2)))
                {
                    if(CheckColliding(Hitbox.Shift(inputDir, 0), true))
                    {
                        MoveY(-2, null);
                        MoveX(inputDir * 2, null);
                    }
                }
                else
                {
                    // if (Math.Abs(velocity.X) >= 1)
                    // {
                    //     _audio_play_sound(sn_player_land, 0, false);
                    //     for (int i = 0; i < 3; i++)
                    //     {
                    //         with(instance_create_depth((x + (4 * sign(facing))), random_range((bbox_bottom - 12), (bbox_bottom - 2)), (depth - 1), fx_dust))
                    //         {
                    //             sprite_index = spr_fx_dust2;
                    //             vy = (Math.Abs(other.velocity.Y) > 0.6) ? other.velocity.Y * 0.5 : vy;
                    //             vz = 0;
                    //         }
                    //     }
                    // }
                    velocity.X = 0;
                    break;
                }
            }
        });
        MoveY(velocity.Y, () => {
            if(State == PlayerState.Normal)
            {
                landTimer = 8;
                textureIndex = inputDir != 0 ? TextureIndex.Run : TextureIndex.Idle;
                frame = 0;
            }
            if (velocity.Y > 0.4f)
            {
                // _audio_play_sound(sn_player_land, 0, false);
                squash = 0.9f;
                stretch = 1.4f;
            }
            // if (velocity.Y > 0.2)
            // {
            //     for (var i = 0; i < 4; i++)
            //     {
            //         with (instance_create_depth((bbox_left + random(8)), random_range(bbox_bottom, bbox_bottom), (depth - 1), fx_dust))
            //         {
            //             sprite_index = spr_fx_dust2
            //             vx = other.velocity.X
            //             vz = 0
            //         }
            //     }
            // }
            if(!(InputMapping.Down.IsDown && CheckCollidingJumpthrough(BottomEdge.Shift(new(0, 1)))))
                velocity.Y = 0;
        });

        if(fxTrail)
        {
            fxTrailCounter++;
            if(fxTrailCounter >= 2)
            {
                fxTrailCounter = 0;
                afterImages.Add(new AfterImage {
                    TextureIndex = textureIndex,
                    Frame = (int)frame,
                    Position = Entity.Position.ToVector2(),
                    Facing = Facing,
                    Scale = Vector2.One,
                    Color = Color.White,
                    Rotation = 0,
                    Pivot = Entity.Pivot.ToVector2(),
                    SpriteEffects = SpriteEffects,
                });
            }
        }
        else
        {
            fxTrailCounter = 0;
        }

        for(int i = 0; i < afterImages.Count; i++)
        {
            AfterImage image = afterImages[i];

            image.Alpha = MathHelper.Max(image.Alpha - (1/12f), 0);
            if(image.Alpha == 0)
            {
                afterImages.RemoveAt(i);
                i--;
            }
        }

        squash = MathUtil.Approach(squash, 1, 0.15f);
        stretch = MathUtil.Approach(stretch, 1, 0.1f);

        if(inputDir != 0 && running && CheckColliding(Hitbox.Shift(inputDir, 0), true))
        {
            textureIndex = TextureIndex.Idle;
        }

        UpdateGun();
    }

    private void UpdateGun()
    {
        if(bulletDelay > 0) bulletDelay--;
        if(bombDelay > 0) bombDelay--;

        recoil = MathHelper.Max(0, recoil - 1);
        if(InputMapping.PrimaryFire.IsDown && bulletDelay == 0)
        {
            Main.Camera.AddShake(1, 5);
            recoil = 2;
            bulletDelay = baseBulletDelay;

            // spawn boolets

            // with (instance_create_depth(x, y, depth - 3, oBullet))
            // {
            // 	parent = obj_player
            //     _team = team.player
            //     audio_play_sound(snShot, 1, false);

            //     speed = 12;
            //     direction = other.image_angle + random_range(-v, v);
            //     image_angle = direction;

            //     damage = obj_player.damage
            // }


            // spawn casing

            // with(instance_create_depth(x + lengthdir_x(4, image_angle), y + lengthdir_y(4, image_angle) - 1, depth - 5, fx_casing))
            // {
            //     image_yscale = other.image_yscale
            //     angle = other.image_angle
            //     dir = other.image_yscale
            //     hsp = -other.image_yscale * random_range(1, 1.5)
            //     vsp = -1 + random_range(-0.2, 0.1)
            // }
        }

        if(InputMapping.SecondaryFire.IsDown && bombDelay == 0)
        {
            // kill bombas

            Main.Camera.AddShake(2, 10);
            recoil = 4;
            bombDelay = baseBombDelay;

            // spawn bombas
        }
    }

    private void OnStateEnter(PlayerState state)
    {
        switch(state)
        {
            case PlayerState.IgnoreState:
                break;
            case PlayerState.StandIdle:
                if(OnGround)
                {
                    textureIndex = TextureIndex.Idle;
                    frame = 0;
                }
                else
                {
                    textureIndex = TextureIndex.Jump;
                }
                break;
            case PlayerState.Normal:
                break;
            case PlayerState.LedgeGrab:
                velocity = Vector2.Zero;
                if(Facing == 0) Facing = 1;
                break;
            case PlayerState.Dead:
                break;
            default:
                break;
        }
    }

    private void OnStateExit(PlayerState state)
    {
        switch(state)
        {
            case PlayerState.IgnoreState:
                break;
            case PlayerState.StandIdle:
                break;
            case PlayerState.Normal:
                break;
            case PlayerState.LedgeGrab:
                ledgegrabTimer = 15;
                break;
            case PlayerState.Dead:
                break;
        }
    }

    private void StateUpdate()
    {
        if(!_stateJustChanged)
        {
            CollidesWithJumpthroughs = true;
            CollidesWithSolids = true;
        }
        else
        {
            _stateJustChanged = false;
        }

        switch(State)
        {
            case PlayerState.StandIdle:
                velocity.X = MathUtil.Approach(velocity.X, 0, fric * 2);

                if(OnGround)
                {
                    textureIndex = TextureIndex.Idle;
                }

                break;
            case PlayerState.Normal: {
                canJump = true;
                canWalljump = true;

                if(duck > 0)
                    SetHitbox(MaskDuck, PivotDuck);
                else
                    SetHitbox(MaskNormal, PivotNormal);

                if(inputDir != 0)
                {
                    Facing = inputDir;

                    if(inputDir * velocity.X < 0)
                    {
                        if(Math.Abs(velocity.X) > moveSpeed * 0.6f)
                            skidding = true;
                        else
                            skidding = false;

                        velocity.X = MathUtil.Approach(velocity.X, 0, fric);
                    }
                    else if(OnGround && velocity.Y >= 0)
                    {
                        skidding = false;
                        if (duck == 0 && landTimer <= 0)
                        {
                            if(Math.Abs(velocity.X) > moveSpeed * 1.3f)
                                textureIndex = TextureIndex.RunFast;
                            else
                                textureIndex = TextureIndex.Run;
                        }
                        else if(duck > 0)
                        {
                            textureIndex = TextureIndex.Crawl;
                        }
                    }

                    if(inputDir * velocity.X < moveSpeed)
                    {
                        velocity.X = MathUtil.Approach(velocity.X, inputDir * moveSpeed, accel);
                    }

                    if(inputDir * velocity.X > moveSpeed && OnGround)
                    {
                        velocity.X = MathUtil.Approach(velocity.X, inputDir * moveSpeed, fric/3);
                    }

                    if(OnGround)
                    {
                        running = true;
                    }
                }
                else
                {
                    running = false;
                    velocity.X = MathUtil.Approach(velocity.X, oldVelocity.X, fric * 2);

                    if (Math.Abs(velocity.X) < moveSpeed)
                    {
                        skidding = false;
                        // run = MathUtil.Approach(run, 0, global.dt)
                    }
                    if (Math.Abs(velocity.X) < 1.5f && OnGround && landTimer <= 0)
                    {
                        bool lookingUp = InputMapping.Up.IsDown;
                        textureIndex = TextureIndex.Idle;
                        if(duck > 0)
                        {
                            textureIndex = TextureIndex.Duck;
                            frame = duck;
                            Lookup = -0.5f;
                        }
                        else if(lookingUp)
                        {
                            textureIndex = TextureIndex.LookUp;
                            Lookup = 1;
                        }
                        else
                        {
                            Lookup = 0;
                        }
                    }
                }

                if (InputMapping.Down.IsDown && OnGround)
                    duck = MathUtil.Approach(duck, 3, 1);
                else if(!CheckColliding(Hitbox.Shift(0, -6)))
                {
                    duck = MathUtil.Approach(duck, 0, 1);
                }

                if(!OnGround)
                {
                    Lookup = 0;

                    if(velocity.Y >= -1f)
                    {
                        if(CheckColliding(Hitbox.Shift(inputDir, 0), true))
                            wallslideTimer++;

                        CheckLedgeGrab();
                    }
                    else
                        wallslideTimer = 0;
                    if (wallslideTimer >= 5)
                        State = PlayerState.Wallslide;

                    jumpBuffer = MathUtil.Approach(jumpBuffer, 0, 1);

                    textureIndex = TextureIndex.Jump;
                    if (velocity.Y >= 0.1)
                        velocity.Y = MathUtil.Approach(velocity.Y, 20, gravity);
                    if (velocity.Y < 0)
                        velocity.Y = MathUtil.Approach(velocity.Y, 20, gravity);
                    else if (velocity.Y < 2)
                        velocity.Y = MathUtil.Approach(velocity.Y, 20, gravity * 0.25f);
                    if (velocity.Y < 0)
                        frame = MathUtil.Approach(frame, 1, 0.2f);
                    else if (velocity.Y >= 0.5)
                        frame = MathUtil.Approach(frame, 5, 0.5f);
                    else
                        frame = 3;
                }
                else
                {
                    if(onJumpthrough && InputMapping.Down.IsDown && !CheckColliding(BottomEdge.Shift(new(0, 2)), true))
                    {
                        Entity.Y += 2;

                        onJumpthrough = CheckCollidingJumpthrough(BottomEdge.Shift(0, 1));
                        if(onJumpthrough) OnGround = true;
                        else OnGround = CheckColliding(BottomEdge.Shift(0, 1));
                    }

                    wallslideTimer = 0;
                    oldVelocity = Vector2.Zero;
                    jumpBuffer = 5;
                }
                if (running && !CheckColliding(Hitbox.Shift(inputDir, 0), true))
                    frame += Math.Abs(velocity.X) / (8f / frameCounts[(int)textureIndex] * 6);
                else if (duck > 0)
                    frame += Math.Abs(velocity.X / 4);
                landTimer = MathUtil.Approach(landTimer, 0, 1);

                fxTrail = Math.Abs(velocity.X) > 1.3f * moveSpeed;

                break;
            }
            case PlayerState.Wallslide: {
                canWalljump = true;
                if (velocity.Y < 0)
                    velocity.Y = MathUtil.Approach(velocity.Y, 20, 0.5f);
                else
                    velocity.Y = MathUtil.Approach(velocity.Y, 20 / 3f, gravity / 3f);
                if (!CheckColliding(Hitbox.Shift(inputDir * 2, 0), true))
                {
                    State = PlayerState.Normal;
                    wallslideTimer = 0;
                }
                else
                {
                    CheckLedgeGrab();
                }
                textureIndex = TextureIndex.Wallslide;
                // var n = choose(0, 1, 0, 1, 1, 0, 0, 0);
                // if(n == 1)
                //     with (instance_create_depth(x + 4 * sign(facing), random_range(bbox_bottom - 12, bbox_bottom), depth - 1, fx_dust))
                //     {
                //         vz = 0
                //         if(instance_exists(other.platformtarget))
                //             vx += other.platformtarget.hsp
                //         sprite_index = spr_fx_dust2
                //     }
                if (inputDir == 0 || OnGround)
                {
                    State = PlayerState.Normal;
                    wallslideTimer = 0;
                }
                if (Math.Sign(inputDir) == -Math.Sign(Facing))
                {
                    State = PlayerState.Normal;
                    wallslideTimer = 0;
                    Facing = Math.Sign(inputDir);
                }
                velocity.Y = MathHelper.Clamp(velocity.Y, -99, 2);
                break;
            }

            case PlayerState.LedgeGrab:
            {
                canLedgeGrab = false;
                canWalljump = false;

                break;
            }

            case PlayerState.IgnoreState: default:
                break;
        }
    }

    private void CheckLedgeGrab()
    {
        var _w = Scene.CollisionSystem.SolidPlace(Hitbox.Shift(inputDir, 0));
        if (canLedgeGrab && ledgegrabTimer == 0 && _w is not null && !CheckColliding(Hitbox))
        {
            if(!CheckColliding(new((inputDir == 1) ? _w.Left + 1 : _w.Right - 1, _w.Top - 1, 1, 1), true)
            && !CheckColliding(new((inputDir == 1) ? _w.Left - 2 : _w.Right + 2, _w.Top + 18, 1, 1), true))
            {
                if (Math.Sign(Top - _w.Top) <= 0 && !CheckColliding(new(Left, _w.Top - 1, Width, Height), true) && !CheckColliding(Hitbox.Shift(0, 2), true))
                {
                    wallslideTimer = 0;
                    State = PlayerState.LedgeGrab;

                    SetHitbox(MaskLedge, PivotLedge);
                    textureIndex = TextureIndex.LedgeGrab;

                    Entity.Y = _w.Top - bboxOffset.Y;
                    Entity.X = ((inputDir == 1) ? _w.Left - Width : _w.Right) - bboxOffset.X;
                    Facing = Math.Sign(_w.Left - Left);
                    platformTarget = _w;

                    return;
                }
            }
        }
    }

    private void Grounded()
    {
        jumpCancelled = false;
    }

    private void RecalculateStats()
    {
        moveSpeed = baseMoveSpeed;
        jumpSpeed = baseJumpSpeed;

        canJump = true;
        canLedgeGrab = true;
        canWalljump = true;

        accel = baseGroundAcceleration;
        fric = baseGroundFriction;
        if(!OnGround)
        {
            accel = baseAirAcceleration;
            fric = baseAirFriction;
            if(Math.Abs(velocity.X) > moveSpeed * 1.3)
                fric *= 0.5f;
        }
    }

    public override void Draw()
    {
        foreach(var image in afterImages)
        {
            if(!Visible) continue;

            var texture = textures[(int)image.TextureIndex];
            Rectangle drawFrame = GraphicsUtil.GetFrameInStrip(texture, image.Frame, frameCounts[(int)image.TextureIndex]);

            if(JellyBackend.DebugEnabled)
            {
                Renderer.SpriteBatch.DrawNineSlice(
                    Main.LoadContent<Texture2D>("Images/Debug/tileOutline"),
                    new Rectangle((int)image.Position.X, (int)image.Position.Y, Width, Height),
                    null,
                    new Point(1),
                    new Point(1),
                    Color.Blue * 0.75f,
                    Vector2.Zero,
                    SpriteEffects.None,
                    0
                );
            }

            Renderer.SpriteBatch.Draw(
                texture,
                image.Position,
                drawFrame,
                image.Color * (image.Alpha * 0.5f),
                image.Rotation,
                image.Pivot,
                image.Scale,
                image.SpriteEffects,
                0
            );
        }

        SpriteEffects spriteEffects = VisualFacing < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

        while(frame > frameCounts[(int)textureIndex])
            frame -= frameCounts[(int)textureIndex];

        {
            var texture = textures[(int)textureIndex];
            Rectangle drawFrame = GraphicsUtil.GetFrameInStrip(texture, frame, frameCounts[(int)textureIndex]);

            Renderer.SpriteBatch.Draw(
                texture,
                Entity.Position.ToVector2(), drawFrame,
                Color.White,
                0, Entity.Pivot.ToVector2(),
                new Vector2(stretch, squash),
                spriteEffects,
                0
            );
        }

        #region Gun Shit Here
        {
            var texture = Main.LoadContent<Texture2D>("Images/Player/gun");

            var vec = (Main.Camera.MousePositionInWorld - Center).ToVector2().SafeNormalize();
            float angle = MathHelper.ToRadians(MathF.Round(MathHelper.ToDegrees(MathF.Atan2(vec.Y, vec.X)) / 10) * 10);

            int x = 0;
            int y = 0;

            switch(textureIndex)
            {
                case TextureIndex.Idle:
                case TextureIndex.LookUp: {
                    x = -3;
                    switch((int)frame % 6)
                    {
                        case 0:
                        case 1:
                        case 2:
                            y = -6;
                            break;
                        case 3:
                        case 4:
                        case 5:
                            y = -7;
                            break;
                    }
                    break;
                }
                case TextureIndex.Run: {
                    x = -3;
                    switch((int)frame % 8)
                    {
                        case 0:
                        case 3:
                        case 4:
                        case 7:
                            y = -6;
                            break;
                        case 1:
                        case 2:
                        case 5:
                        case 6:
                            y = -5;
                            break;
                    }
                    break;
                }
                case TextureIndex.RunFast: {
                    x = -3;
                    switch((int)frame % 6)
                    {
                        case 0: x = -3; y = -6; break;
                        case 1: x = -2; y = -5; break;
                        case 2: x = -1; y = -6; break;
                        case 3: x = -0; y = -6; break;
                        case 4: x = -0; y = -5; break;
                        case 5: x = -1; y = -6; break;
                    }
                    break;
                }
                case TextureIndex.Crawl: {
                    switch((int)frame % 8)
                    {
                        case 0: x = -2; y = -2; break;
                        case 1: x = -4; y = -2; break;
                        case 2: x = -5; y = -2; break;
                        case 3: x = -4; y = -2; break;
                        case 4: x = -3; y = -3; break;
                        case 5: x = -1; y = -3; break;
                        case 6: x =  1; y = -2; break;
                        case 7: x =  0; y = -2; break;
                    }
                    break;
                }
                default: {
                    if(State == PlayerState.Wallslide)
                    {
                        x = 3;
                        y = -7;
                    }
                    else if(State == PlayerState.LedgeGrab)
                    {
                        x = -4;
                        y = 3;
                    }
                    else if(State == PlayerState.LedgeClimb)
                    {
                        x = -4;
                        y = -5;
                    }
                    else if(duck > 0)
                    {
                        x = -3 + (int)(1/3f * duck);
                        y = -5 + (int)duck;
                    }
                    else
                    {
                        x = -3;
                        y = -7;
                    }
                    break;
                }
            }

            Renderer.SpriteBatch.Draw(
                texture,
                new Vector2(
                    Entity.X + x * stretch * VisualFacing + -recoil * MathF.Cos(angle),
                    Entity.Y + y * squash + -recoil * MathF.Sin(angle)
                ),
                null,
                Color.White,
                angle,
                new Vector2(2, 8),
                new Vector2(stretch, squash),
                (SpriteEffects)((((int)spriteEffects) << 1) & 2),
                0
            );
        }
        #endregion

        if(JellyBackend.DebugEnabled)
        {
            Renderer.SpriteBatch.DrawNineSlice(Main.LoadContent<Texture2D>("Images/Debug/tileOutline"), Hitbox, null, new Point(1), new Point(1), Color.Red * 0.5f);
        }
    }

    public override void DrawUI()
    {
        if(!JellyBackend.DebugEnabled) return;

        Renderer.SpriteBatch.DrawStringSpacesFix(MasterRenderer.Fonts.RegularFont, State.ToString(), new Vector2(1, 1), Color.White, 6);
    }
}

public class PlayerInputMapping
{
    public MappedInput Right { get; set; } = new MappedInput.Keyboard(Keys.D);
    public MappedInput Left { get; set; } = new MappedInput.Keyboard(Keys.A);
    public MappedInput Down { get; set; } = new MappedInput.Keyboard(Keys.S);
    public MappedInput Up { get; set; } = new MappedInput.Keyboard(Keys.W);
    public MappedInput Jump { get; set; } = new MappedInput.Keyboard(Keys.Space);
    public MappedInput PrimaryFire { get; set; } = new MappedInput.Mouse(MouseButtons.LeftButton);
    public MappedInput SecondaryFire { get; set; } = new MappedInput.Mouse(MouseButtons.RightButton);
}

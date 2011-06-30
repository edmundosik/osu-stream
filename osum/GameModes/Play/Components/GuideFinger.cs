﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using osum.Graphics.Skins;
using osum.Graphics.Sprites;
using OpenTK;
using osum.GameplayElements;
using osum.GameplayElements.HitObjects.Osu;
using osum.Helpers;
using OpenTK.Graphics;
using osum.Graphics;
using osum.Audio;

namespace osum.GameModes.Play.Components
{
    class GuideFinger : GameComponent
    {
        List<pDrawable> fingers = new List<pDrawable>();


        pSprite leftFinger;

        internal HitObjectManager HitObjectManager;
        internal TouchBurster TouchBurster;
        private pSprite rightFinger;
        private pSprite leftFinger2;
        private pSprite rightFinger2;
        public override void Initialize()
        {
            leftFinger = new pSprite(TextureManager.Load(OsuTexture.finger_inner), new Vector2(-100, 200))
            {
                Field = FieldTypes.GamefieldSprites,
                Origin = OriginTypes.Centre,
                Colour = ColourHelper.Lighten2(Color4.LimeGreen, 0.5f),
                Alpha = 0.3f,
                Additive = true
            };

            leftFinger2 = new pSprite(TextureManager.Load(OsuTexture.finger_outer), Vector2.Zero)
            {
                Field = FieldTypes.GamefieldSprites,
                Origin = OriginTypes.Centre,
                Colour = Color4.LimeGreen,
                Alpha = 0.3f,
                Additive = false
            };

            rightFinger = new pSprite(TextureManager.Load(OsuTexture.finger_inner), new Vector2(612, 200))
            {
                Field = FieldTypes.GamefieldSprites,
                Origin = OriginTypes.Centre,
                Colour = ColourHelper.Lighten2(Color4.Red, 0.5f),
                Alpha = 0.3f,
                Additive = true
            };

            rightFinger2 = new pSprite(TextureManager.Load(OsuTexture.finger_outer), Vector2.Zero)
            {
                Field = FieldTypes.GamefieldSprites,
                Origin = OriginTypes.Centre,
                Colour = Color4.Red,
                Alpha = 0.3f,
                Additive = false
            };

            leftFinger.Transform(new Transformation(TransformationType.Movement, new Vector2(-100, 130), new Vector2(50, 200), 0, 800, EasingTypes.In));
            rightFinger.Transform(new Transformation(TransformationType.Movement, new Vector2(612, 130), new Vector2(512 - 50, 200), 0, 800, EasingTypes.In));

            spriteManager.Add(leftFinger);
            spriteManager.Add(rightFinger);
            spriteManager.Add(leftFinger2);
            spriteManager.Add(rightFinger2);

            fingers.Add(leftFinger);
            fingers.Add(rightFinger);

            base.Initialize();
        }

        public override bool Draw()
        {
            if (HitObjectManager == null) return false;

            return base.Draw();
        }

        public override void Update()
        {
            base.Update();

            if (HitObjectManager == null) return;

            HitObject nextObject = HitObjectManager.NextObject;
            HitObject nextObjectConnected = nextObject != null ? nextObject.connectedObject : null;

            bool objectHasFinger = false;
            bool connectedObjectHasFinger = false;

            foreach (pDrawable finger in fingers)
            {
                HitObject obj = finger.Tag as HitObject;

                if (obj != null)
                {
                    if (obj == nextObject) objectHasFinger = true;
                    if (obj == nextObjectConnected) connectedObjectHasFinger = true;

                    if (obj.IsHit)
                    {
                        finger.Tag = null;
                        finger.FadeOut(800, 0.3f);
                        finger.MoveTo(new Vector2(finger == leftFinger ? 50 : 512 - 50, 200), 800, EasingTypes.InOut);
                    }
                    else if (obj.IsActive)
                    {
                        finger.Position = obj.TrackingPosition;

                        if (TouchBurster != null && AudioEngine.Music.IsElapsing)
                            TouchBurster.Burst(GameBase.GamefieldToStandard(finger.Position + finger.Offset), 40, 0.5f, 1);
                    }
                    else if (obj.IsVisible)
                    {
                        int timeUntilObject = obj.StartTime - Clock.AudioTime;

                        if (timeUntilObject < 350)
                        {
                            Vector2 src = finger.Position;
                            Vector2 dest = obj.TrackingPosition;

                            lastFinger = finger;

                            finger.Position = src + (dest - src) * 0.015f * (float)GameBase.ElapsedMilliseconds;

                            float vOffset = 0;
                            if (timeUntilObject > 100)
                                vOffset = (1 - pMathHelper.ClampToOne((timeUntilObject - 100) / 300f));
                            else
                                vOffset = pMathHelper.ClampToOne(timeUntilObject / 100f);

                            finger.Offset.Y = vOffset * -55;
                            finger.ScaleScalar = 1 + 0.6f * vOffset;

                            if (TouchBurster != null)
                                TouchBurster.Burst(GameBase.GamefieldToStandard(finger.Position + finger.Offset), 40, 0.5f, 1);
                        }
                    }
                }
            }

            {
                int timeUntilObject = nextObject == null ? Int32.MaxValue : nextObject.StartTime - Clock.AudioTime;

                if (timeUntilObject < 500)
                {
                    if (!objectHasFinger) checkObject(nextObject);
                    if (nextObjectConnected != null && !connectedObjectHasFinger) checkObject(nextObjectConnected);
                }
            }

            leftFinger2.Position = leftFinger.Position;
            leftFinger2.Offset = leftFinger.Offset;
            leftFinger2.ScaleScalar = leftFinger.ScaleScalar;
            leftFinger2.Alpha = leftFinger.Alpha;

            rightFinger2.Position = rightFinger.Position;
            rightFinger2.Offset = rightFinger.Offset;
            rightFinger2.ScaleScalar = rightFinger.ScaleScalar;
            rightFinger2.Alpha = rightFinger.Alpha;
        }

        pDrawable lastFinger;
        HitObject lastObject = null;

        private void checkObject(HitObject nextObject)
        {
            pDrawable preferred = null;

            float leftPart = GameBase.GamefieldBaseSize.Width / 11f * 4;
            float rightPart = GameBase.GamefieldBaseSize.Width / 11f * 7;

            float distFromLeft = pMathHelper.Distance(nextObject.Position, leftFinger.Tag == null ? leftFinger.Position : ((HitObject)leftFinger.Tag).EndPosition);
            float distFromRight = pMathHelper.Distance(nextObject.Position, rightFinger.Tag == null ? rightFinger.Position : ((HitObject)rightFinger.Tag).EndPosition);

            if (nextObject.connectedObject != null)
            {
                //if there is a connected object, always use the correct L-R arrangement.
                if (nextObject.Position.X == nextObject.connectedObject.Position.X)
                {
                    // if same x we'll assign the closest finger to each note
                    float connectedDistFromLeft = pMathHelper.Distance(nextObject.connectedObject.Position, leftFinger.Tag == null ? leftFinger.Position : ((HitObject)leftFinger.Tag).EndPosition);
                    float connectedDistFromRight = pMathHelper.Distance(nextObject.connectedObject.Position, rightFinger.Tag == null ? rightFinger.Position : ((HitObject)rightFinger.Tag).EndPosition);

                    float smallest = Math.Min(Math.Min(connectedDistFromLeft, connectedDistFromRight), Math.Min(distFromLeft, distFromLeft));
                    preferred = smallest == distFromLeft || smallest == connectedDistFromRight ? leftFinger : rightFinger;
                }
                else if (nextObject.Position.X < nextObject.connectedObject.Position.X)
                    preferred = leftFinger;
                else
                    preferred = rightFinger;
            }

            else if (distFromLeft < 20)
                //stacked objects (left finger)
                preferred = leftFinger;
            else if (distFromRight < 20)
                //stacked objects (right finger)
                preferred = rightFinger;

            else if (lastObject != null && lastObject != nextObject && !(lastObject is Slider) && !(nextObject is Slider) && nextObject.StartTime - lastObject.EndTime < 150)
                //fast hits; always alternate fingers
                preferred = lastFinger == leftFinger ? rightFinger : leftFinger;

            /*
            else if (nextObject.Position.X > nextObject.Position2.X && nextObject.Position.X < rightPart && Math.Abs(nextObject.Position.Y - nextObject.Position2.Y) < 20)
                //sliders that start right and end left, centered towards the left
                preferred = leftFinger;
            else if (nextObject.Position.X < nextObject.Position2.X && nextObject.Position.X > leftPart && Math.Abs(nextObject.Position.Y - nextObject.Position2.Y) < 20)
                //sliders that start left and end right, centered towards the right
                preferred = rightFinger;
            */

            else if (nextObject.Position.X < leftPart)
                //starts in left 1/3 of screen.
                preferred = leftFinger;
            else if (nextObject.Position.X > rightPart)
                //starts in right 1/3 of screen.
                preferred = rightFinger;
            else if (nextObject.Position2.X < leftPart)
                //ends in left 1/3 of screen.
                preferred = leftFinger;
            else if (nextObject.Position2.X > rightPart)
                //ends in right 1/3 of screen.
                preferred = rightFinger;

            else if (lastObject is HoldCircle)
                //hold note; always alternate fingers
                preferred = lastFinger == leftFinger ? rightFinger : leftFinger;

            else
                //fall back to the closest finger.
                preferred = distFromLeft < distFromRight ? leftFinger : rightFinger;

            if (preferred == leftFinger && nextObject.Position.X > rightFinger.Position.X && rightFinger.Tag == null)
                //if we're about to use left finger but the object is wedged between the right finger and right side of screen, use right instead.
                preferred = rightFinger;
            else if (preferred == rightFinger && nextObject.Position.X < leftFinger.Position.X && leftFinger.Tag == null)
                //if we're about to use right finger but the object is wedged between the left finger and left side of screen, use left instead.
                preferred = leftFinger;

            pDrawable alternative = preferred == leftFinger ? rightFinger : leftFinger;

            if (preferred.Tag == null)
            {
                preferred.Tag = nextObject;
                preferred.Transformations.Clear();
                preferred.FadeIn(300);
            }
            else
            {
                //finger is bxusy...
                HitObject busyObject = preferred.Tag as HitObject;

                if (busyObject.EndTime > nextObject.StartTime - 80)
                {
                    alternative.Tag = nextObject;
                    alternative.Transformations.Clear();
                    alternative.FadeIn(300);
                }
            }

            lastObject = nextObject;
        }

    }
}
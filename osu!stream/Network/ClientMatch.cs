﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using osum.Helpers.osu_common.Tencho.Objects;
using osu_common.Helpers;
using osu_common.Tencho.Requests;
using osum.GameModes;
using osu_common.Tencho.Objects;
using osum.GameplayElements.Beatmaps;
using OpenTK;

namespace osum.Network
{
    class ClientMatch : bMatch
    {
        public ClientMatch(SerializationReader sr)
            : base(sr)
        {

        }

        internal void RequestStateChange(MatchState state)
        {
            GameBase.Client.SendRequest(RequestType.Stream_RequestStateChange, new bInt((int)state));
        }

        internal void Update(RequestType reqType, ClientMatch newInfo)
        {
            switch (reqType)
            {
                case RequestType.Tencho_MatchPlayerDataChange:
                    Players = newInfo.Players;

                    if (Players.Count < 2 && Director.CurrentOsuMode != OsuMode.Multiplay)
                        Director.ChangeMode(OsuMode.Multiplay);

                    break;
                case RequestType.Tencho_MatchStateChange:
                    State = newInfo.State;

                    switch (State)
                    {
                        case MatchState.SongSelect:
                            if (Director.CurrentOsuMode != OsuMode.SongSelect)
                                Director.ChangeMode(OsuMode.SongSelect);
                            else
                            {
                                ((SongSelectMode)Director.CurrentMode).ChangeMap(null);
                            }
                            break;
                        case MatchState.DifficultySelect:
                            Beatmap = newInfo.Beatmap;
                            SongSelectMode s = Director.CurrentMode as SongSelectMode;
                            if (s != null)
                            {
                                s.ChangeMap(Beatmap);
                            }
                            break;
                        case MatchState.Preparing:
                            Director.ChangeMode(OsuMode.Play);
                            break;
                        case MatchState.Playing:
                            Player p = Director.CurrentMode as Player;
                            if (p != null)
                            {
                                p.Start();
                            }
                            break;
                    }

                    break;
            }
        }

        internal void RequestSong(Beatmap beatmap)
        {
            GameBase.Client.SendRequest(RequestType.Stream_RequestSong, beatmap ?? new bBeatmap());
        }

        internal void SendInput(List<TrackingPoint> TrackingPoints, bool isDown = false)
        {
            bPlayerData data = new bPlayerData();

            //if (isDown)
                data.Input.AddRange(TrackingPoints);
            //else
            //{
            //    foreach (TrackingPoint p in TrackingPoints)
            //    {
            //        if (p.WindowDelta == Vector2.Zero)
            //        {
            //            TrackingPoint clone = (TrackingPoint)p.Clone();
            //            clone.WindowDelta = new Vector2(-1, -1);
            //            data.Input.Add(clone);
            //        }
            //        else
            //            data.Input.Add(p);
            //    }
            //}

            GameBase.Client.SendRequest(RequestType.Stream_InputUpdate, data);
        }
    }
}
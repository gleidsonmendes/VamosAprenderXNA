﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ElemarJR.Xna
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    public class GameModel : IDrawableModel
    {
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Scale { get; set; }

        public Vector3 BaseRotation { get; set; }

        public Model Model { get; private set; }

        Matrix[] modelTransforms;

        public GameModel(
            Model Model
            )
        {
            this.Model = Model;

            modelTransforms = new Matrix[Model.Bones.Count];
            Model.CopyAbsoluteBoneTransformsTo(modelTransforms);

            this.baseBoundingSphere = Model.ComputeBoundingSphere();
            this.LoadTags();

            this.Position = Vector3.Zero;
            this.Rotation = Vector3.Zero;
            this.Scale = Vector3.One;
        }

        void LoadTags()
        {
            foreach (var mesh in Model.Meshes)
                foreach (var part in mesh.MeshParts)
                    if (part.Effect is BasicEffect)
                    {
                        BasicEffect effect = (BasicEffect)part.Effect;
                        part.Tag = new MeshTag(
                            effect.DiffuseColor,
                            effect.Texture,
                            effect.SpecularPower
                            );
                    }
        }

        private void ExecuteOnAllEffects(Action<Effect> action)
        {
            foreach (var mesh in Model.Meshes)
                foreach (var part in mesh.MeshParts)
                {
                    action(part.Effect);
                }
        }

        public void UpdateCameraPosition(Vector3 cameraPos)
        {
            this.ExecuteOnAllEffects( (effect) => 
                {
                    if (effect.Parameters["CameraPosition"] != null)
                        effect.Parameters["CameraPosition"].SetValue(cameraPos);
                }
                );
        }

        public void UpdateClipPlane(Vector4? plane)
        {
            this.ExecuteOnAllEffects((effect) =>
            {
                if (effect.Parameters["ClipPlaneEnabled"] != null)
                    effect.Parameters["ClipPlaneEnabled"].SetValue(plane.HasValue);

                if (plane.HasValue && effect.Parameters["ClipPlane"] != null)
                    effect.Parameters["ClipPlane"].SetValue(plane.Value);
            });
        }

        public GameModel CacheEffects()
        {
            foreach (var mesh in Model.Meshes)
                foreach (var part in mesh.MeshParts)
                {
                    var tag = (MeshTag)part.Tag;
                    tag.CachedEffect = part.Effect;
                }

            return this;
        }

        public GameModel RestoreCachedEffects()
        {
            foreach (var mesh in Model.Meshes)
                foreach (var part in mesh.MeshParts)
                {
                    var tag = (MeshTag)part.Tag;
                    if (tag.CachedEffect != null)
                        part.Effect = tag.CachedEffect;
                }

            return this;
        }

        public GameModel SetEffect(Effect effect, 
            bool clone = true, 
            bool allowSpecular = true)
        {
            foreach (var mesh in Model.Meshes)
                foreach (var part in mesh.MeshParts)
                {
                    var to = (clone ? effect.Clone() : effect);
                    var tag = (MeshTag)part.Tag;

                    if (tag.Texture != null)
                    {
                        if (to.Parameters["BasicTexture"] != null)
                            to.Parameters["BasicTexture"].SetValue(tag.Texture);
                        if (to.Parameters["TextureEnabled"] != null)
                            to.Parameters["TextureEnabled"].SetValue(true);
                    }
                    else
                    {
                        if (to.Parameters["TextureEnabled"] != null)
                            to.Parameters["TextureEnabled"].SetValue(false);
                    }

                    if (to.Parameters["DiffuseColor"] != null)
                        to.Parameters["DiffuseColor"].SetValue(tag.Color);


                    if (to.Parameters["SpecularPower"] != null)
                        to.Parameters["SpecularPower"].SetValue(tag.SpecularPower);


                    if (to.Parameters["SpecularEnabled"] != null)
                        to.Parameters["SpecularEnabled"].SetValue(allowSpecular);

                    part.Effect = to;
                }
            return this;
        }


        public static implicit operator GameModel(Model @model)
        {
            return new GameModel(@model);
        }


        private BoundingSphere baseBoundingSphere;
        public BoundingSphere BoundingSphere
        {
            get
            {
                return baseBoundingSphere.Transform(
                    ComputeWorld()
                    );
            }
        }

        Matrix ComputeWorld()
        {
            return
                Matrix.CreateFromYawPitchRoll(
                    BaseRotation.Y,
                    BaseRotation.X,
                    BaseRotation.Z
                    ) *
                Matrix.CreateScale(Scale) *
                Matrix.CreateFromYawPitchRoll(
                    Rotation.Y,
                    Rotation.X,
                    Rotation.Z
                    ) *
                Matrix.CreateTranslation(Position);
        }

        public void Draw(Matrix View, Matrix Projection)
        {
            Matrix baseWorld = ComputeWorld();


            foreach (ModelMesh mesh in Model.Meshes)
            {
                Matrix localWorld = modelTransforms[mesh.ParentBone.Index]
                    * baseWorld;
                mesh.SetupEffects(localWorld, View, Projection);
                mesh.Draw();
            }
        }
    }
}

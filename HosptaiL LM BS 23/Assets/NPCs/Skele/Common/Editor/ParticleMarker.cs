using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#pragma warning disable 618

namespace MH
{
    /// <summary>
    /// used to draw particles at specified positions
    /// </summary>
	public class ParticleMarker
	{
	    #region "data"
        // data
        private ParticleSystem m_PS = null;
        private ParticleSystem.Particle[] m_Particles = null;
        private Renderer m_Renderer;

        private int m_PCnt = 0; //the in-use particle counts

        #endregion "data"

	    #region "public method"
        // public method

        public ParticleMarker()
        {

        }

        public void Init(int vcnt, Material customMaterial = null)
        {
            GameObject go = new GameObject("__particle_marker");
            //go.hideFlags = HideFlags.HideAndDontSave;
            m_PS = go.AddComponent<ParticleSystem>();
            m_PS.simulationSpace = ParticleSystemSimulationSpace.World;
            m_PS.startLifetime = float.MaxValue*0.1f;
            m_PS.maxParticles = 1000000;

            m_Renderer = go.GetComponent<Renderer>();
            Dbg.Assert(m_Renderer != null, "ParticleMarker.Init: failed to get renderer");

            if (customMaterial != null)
                m_Renderer.sharedMaterial = customMaterial;

            m_Particles = new ParticleSystem.Particle[vcnt];

            ParticleSystemRenderer psr = m_PS.GetComponent<Renderer>() as ParticleSystemRenderer;
            psr.maxParticleSize = MAX_PARTICLE_SCREEN_SIZE;

            m_PS.enableEmission = false;
            m_PS.Emit(vcnt);
            m_PS.GetParticles(m_Particles);
            m_PS.SetParticles(m_Particles, 0);
            m_PS.Simulate(0.1f); //Simulate will pause the PS, required

        }

        public void Fini()
        {
            if( Application.isPlaying )
            {
                GameObject.Destroy(m_PS.gameObject);
            }
            else
            {
                GameObject.DestroyImmediate(m_PS.gameObject);
            }
        }

        public ParticleSystemSimulationSpace Space
        {
            get { return m_PS.simulationSpace; }
            set { m_PS.simulationSpace = value; }
        }

        public ParticleSystem.Particle[] Particles
        {
            get { return m_Particles; }
        }

        public int ParticleCount
        {
            get { return m_PCnt; }
            set { m_PCnt = value; }
        }

        public Renderer renderer
        {
            get { return m_Renderer; }
        }

        public void Apply(int particleCnt)
        {
            ParticleCount = particleCnt;
            m_PS.SetParticles(m_Particles, m_PCnt);

            //Dbg.Log("PCnt = {0}", m_PCnt);
        }

        //public void Apply()
        //{
        //    m_PS.SetParticles(m_Particles, m_PCnt);
        //}

        //public void Update()
        //{
        //    m_PS.Simulate(0f);
        //}

        #endregion "public method"

	    #region "private method"
        // private method

        #endregion "private method"

	    #region "constant data"
        // constant data

        private const float MAX_PARTICLE_SCREEN_SIZE = 0.01f;

        #endregion "constant data"
    
	}
}

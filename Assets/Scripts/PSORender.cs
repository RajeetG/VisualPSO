﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PSORender : MonoBehaviour
{
    public TextAsset    runData;
    public TextAsset    functionData;
    public TextAsset    topologyData;
    public float        timePerIteration = 1.0f;
    public PSOParticle  particlePrefab;
    public PSOFunction  functionPrefab;
    public Gradient     colorParticles;
    public bool         moveY = false;
    public float        yScale = 1.0f;
    public bool         displayConnectivity = true;
    [Range(0.0f, 10.0f)]
    public float        playSpeed = 1.0f;

    [Header("References")]
    public Camera       mainCamera;

    [Header("Runtime")]
    public Rect         boundary;
    public Vector2      extentsY;
    public float        totalTime;

    List<PSOParticle> particles;

    [HideInInspector] public string functionText = "";
    [HideInInspector] public string runText = "";
    [HideInInspector] public string topologyText = "";

    void Start()
    {
        particles = new List<PSOParticle>();

        var splitFile = new string[] { "\r\n", "\r", "\n" };
        var splitLine = new string[] { ";" };

        string rd = (runData == null)?(runText):(runData.text);

        var lines = rd.Split(splitFile, System.StringSplitOptions.RemoveEmptyEntries);

        float x1 = float.MaxValue;
        float z1 = float.MaxValue;
        float x2 = -float.MaxValue;
        float z2 = -float.MaxValue;

        extentsY.Set(float.MaxValue, -float.MaxValue);

        totalTime = 0.0f;
        boundary.Set(0, 0, 0, 0);

        if (lines.Length > 1)
        {
            for (int idx = 1; idx < lines.Length; idx++)
            {
                var line = lines[idx];
                var tokens = line.Split(splitLine, System.StringSplitOptions.None);
                int action = int.Parse(tokens[0]);
                int iteration = int.Parse(tokens[1]);
                int particleId = int.Parse(tokens[2]);
                float x = float.Parse(tokens[3]);
                float z = float.Parse(tokens[4]);
                float y = float.Parse(tokens[5]);

                x1 = Mathf.Min(x1, x);
                x2 = Mathf.Max(x2, x);
                z1 = Mathf.Min(z1, z);
                z2 = Mathf.Max(z2, z);

                extentsY.x = Mathf.Min(extentsY.x, y);
                extentsY.y = Mathf.Max(extentsY.y, y);

                totalTime = Mathf.Max(totalTime, iteration * timePerIteration);

                if (particles.Count <= particleId)
                {
                    for (int i = particles.Count; i <= particleId; i++) particles.Add(null);
                }

                if (particles[particleId] == null)
                {
                    // Create new particle
                    particles[particleId] = Instantiate(particlePrefab);
                    particles[particleId].name = "Particle " + particleId;
                    particles[particleId].particleId = particleId;
                    particles[particleId].transform.position = new Vector3(x, 0, z);
                    particles[particleId].manager = this;
                }

                if (action == 0)
                {
                    if (!moveY) y = 0.0f;
                    particles[particleId].AddUpdateAction(iteration * timePerIteration, x, y * yScale, z);
                }
                else
                {
                    Debug.Assert(false, "Unknown action!");
                }
            }

            boundary.Set(x1, z1, x2 - x1, z2 - z1);

            float maxExtent = Mathf.Max(boundary.height, boundary.width);
            if (mainCamera.orthographic)
            {
                mainCamera.orthographicSize = (maxExtent * 0.5f) * 1.05f;
            }

            float scale = maxExtent / 100.0f;
            foreach (var particle in particles)
            {
                particle.scale = scale;
                particle.color = colorParticles.Evaluate(Random.Range(0.0f, 1.0f));
                particle.totalTime = totalTime;
            }
        }

        if (((topologyData) || (topologyText != "")) && (displayConnectivity))
        {
            string td = (topologyData == null) ? (topologyText) : (topologyData.text);
            lines = td.Split(splitFile, System.StringSplitOptions.RemoveEmptyEntries);

            for (int idx = 0; idx < lines.Length; idx++)
            {
                var line = lines[idx];
                var tokens = line.Split(splitLine, System.StringSplitOptions.None);

                var particleId = int.Parse(tokens[0]);
                //if (particleId != 0) continue;

                for (int i = 1; i < tokens.Length; i++)
                {
                    particles[particleId].AddConnection(particles[int.Parse(tokens[i])]);
                }
            }
        }

        if (((functionData) || (functionText != "")) && (functionPrefab))
        {
            string fd = (functionData == null) ? (functionText) : (functionData.text);

            var visFunction = Instantiate(functionPrefab);
            visFunction.manager = this;
            visFunction.Parse(fd, yScale);
        }
    }

    public List<PSOParticle> GetParticles()
    {
        return particles;
    }

    public PSOParticle GetRandomParticle()
    {
        if (particles == null) return null;

        return particles[Random.Range(0, particles.Count)];
    }

    public PSOParticle GetBestParticle()
    {
        if (particles == null) return null;

        float       val = float.MaxValue;
        PSOParticle particle = null;

        foreach (var p in particles)
        {
            if (p.transform.position.y < val)
            {
                particle = p;
                val = p.transform.position.y;
            }
        }

        return particle;
    }

    public PSOParticle GetWorstParticle()
    {
        if (particles == null) return null;

        float val = -float.MaxValue;
        PSOParticle particle = null;

        foreach (var p in particles)
        {
            if (p.transform.position.y > val)
            {
                particle = p;
                val = p.transform.position.y;
            }
        }

        return particle;
    }
}

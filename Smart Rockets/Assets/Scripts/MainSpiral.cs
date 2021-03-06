﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MainSpiral : MonoBehaviour {
    // Start is called before the first frame update
    public int numRockets;
    public GameObject rocketPrefab;
    private GameObject[] rockets;
    private MissileControlSpiral[] rocketsControl;
    public float speed;
    public Transform goalTransform;
    private int numGenes = 1000;
    private float geneRange = 40f;
    public Transform startPos;
    public Transform[] mileStones = new Transform[70];
    private bool[] passedMilestone = new bool[70];
    public int currentMilestoneLevel = 0;
    public int currentGen = 0;
    public int currentRange = 0;
    private int prevRange = 0;
    private int numMilestones = 70;
    public bool finishedTraining;
    public float slerpRate;
    public int mutationRate;
    private bool SetUpRockets = true;
    public bool m48 = false;
    private GenerationDisplaySpiral generationDisplay;

    public bool startSimulationBool = false;
    private GameObject slider1;
    private GameObject slider2;
    private GameObject slider3;
    private GameObject slider4;
    private GameObject button1;


    void Start() {
        slerpRate = 5f;
        mutationRate = 5;
        foreach (Transform child in transform) {
            if (child.tag == "Canvas")
            {
                foreach (Transform grandChild in child)
                {
                    if (grandChild.tag == "Text")
                    {
                        generationDisplay = grandChild.gameObject.GetComponent<GenerationDisplaySpiral>();
                    }
                }
            }

            if (child.tag == "Goal") {
                goalTransform = child;
            }
        }
        currentGen = 1;
        generationDisplay.genText.text = "Generation: " + currentGen;
        slider1 = GameObject.Find("Slider-speed");
        slider2 = GameObject.Find("Slider-mutate");
        slider3 = GameObject.Find("Slider-population");
        slider4 = GameObject.Find("Slider-slerp");
        button1 = GameObject.Find("Button - start sim");
    }
    float[] createForces() {
        float[] forces = new float[numGenes];
        for (int i = 0; i < numGenes; i++) {
            forces[i] = UnityEngine.Random.Range(0f, geneRange);
        }
        return forces;
    }
    // Update is called once per frame
    void Update() {
        if (currentMilestoneLevel >= 48) {
            m48 = true;
        }
        if (startSimulationBool == true) {
            slider1.SetActive(false);
            slider2.SetActive(false);
            slider3.SetActive(false);
            slider4.SetActive(false);
            button1.SetActive(false);
            if (SetUpRockets) {
                rockets = new GameObject[numRockets];
                rocketsControl = new MissileControlSpiral[numRockets];
                Quaternion rotation = new Quaternion(0, 0, 0, 1);
                for (int i = 0; i < numRockets; i++) {
                    rockets[i] = Instantiate(rocketPrefab, startPos.position, rotation) as GameObject;
                    rocketsControl[i] = rockets[i].GetComponentInChildren<MissileControlSpiral>();
                    rocketsControl[i].isReady = true;
                    rocketsControl[i].thrusterLeftForces = createForces();
                    rocketsControl[i].thrusterRightForces = createForces();
                    rocketsControl[i].goalTransform = goalTransform;
                    rocketsControl[i].mileStones = mileStones;
                    rocketsControl[i].speed = speed;
                    rocketsControl[i].numGenes = numGenes;
                    rocketsControl[i].slerpRate = slerpRate;
                    rocketsControl[i].m48 = m48;
                }
                SetUpRockets = false;
            } else {
                if (finished(rocketsControl)) {
                    float percentReachedGoal = percentFinished(rocketsControl);
                    if (percentReachedGoal >= .9) {
                        finishedTraining = true;
                        generationDisplay.genText.text = "Generation: " + currentGen + "\n Finished training in " + currentGen + " generations!";
                    }
                    if (!finishedTraining) {
                        float[] crashPos = rocketsControl[0].crashPos;
                        bool samePos = true;
                        for (int i = 0; i < numRockets; i++) {
                            samePos = samePos && ((Math.Abs(rocketsControl[i].crashPos[0] - crashPos[0]) <= .1) && (Math.Abs(rocketsControl[i].crashPos[1] - crashPos[1]) <= .1));
                        }
                        if (samePos) {
                            Quaternion rotation = new Quaternion(0, 0, 0, 1);
                            for (int i = 0; i < numRockets; i++) {
                                float[][] forces = new float[][] { rocketsControl[i].thrusterLeftForces, rocketsControl[i].thrusterRightForces };
                                Destroy(rockets[i]);
                                rockets[i] = Instantiate(rocketPrefab, startPos.position, rotation) as GameObject;
                                rocketsControl[i] = rockets[i].GetComponentInChildren<MissileControlSpiral>();
                                rocketsControl[i].thrusterLeftForces = forces[0];
                                rocketsControl[i].thrusterRightForces = forces[1];
                                rocketsControl[i].goalTransform = goalTransform;
                                rocketsControl[i].speed = speed;
                                rocketsControl[i].mileStones = mileStones;
                                rocketsControl[i].numGenes = numGenes;
                                rocketsControl[i].slerpRate = slerpRate;
                                rocketsControl[i].m48 = m48;
                            }
                            for (int i = 0; i < numRockets; i++) {
                                rocketsControl[i].isReady = true;
                            }
                        } else {
                            double totalFitness = 0;
                            //get total fitness of all rockets
                            double maxFitness = 0;
                            int mostFit = 0;
                            for (int i = 0; i < rocketsControl.Length; i++) {
                                if (rocketsControl[i].fitness > maxFitness) {
                                    maxFitness = rocketsControl[i].fitness;
                                    mostFit = i;
                                }
                                totalFitness += rocketsControl[i].fitness;
                            }
                            //normalize rocket fitness and get a fraction of 200
                            for (int i = 0; i < rocketsControl.Length; i++) {
                                rocketsControl[i].fitness = Math.Floor((rocketsControl[i].fitness / totalFitness) * 1000);
                            }
                            List<MissileControlSpiral> matingpool = new List<MissileControlSpiral>();
                            for (int i = 0; i < numRockets; i++) {
                                for (int j = 0; j < rocketsControl[i].fitness; j++) {
                                    matingpool.Add(rocketsControl[i]);
                                }
                            }
                            mutationRange();
                            destroyAndCreate(matingpool, rocketsControl[mostFit]);
                        }
                        currentGen++;
                        generationDisplay.genText.text = "Generation: " + currentGen;
                    }
                }
            }
        }
    }
    float percentFinished(MissileControlSpiral[] mce) {
        int numReachedGoal = 0;
        for (int i = 0; i < mce.Length; i++) {
            if (mce[i].reachedGoal) {
                numReachedGoal++;
            }
        }
        return (float)numReachedGoal / (float)numRockets;
    }
    bool finished(MissileControlSpiral[] rc) {
        bool finished = true;
        for (int i = 0; i < numRockets; i++) {
            finished = finished && rc[i].finished;
            if (!finished) {
                return finished;
            }
        }
        return finished;
    }
    void destroyAndCreate(List<MissileControlSpiral> matingpool, MissileControlSpiral mostFit) {
        Quaternion rotation = new Quaternion(0, 0, 0, 1);
        //assign random mass?
        Destroy(rockets[0]);
        rockets[0] = Instantiate(rocketPrefab, startPos.position, rotation) as GameObject;
        rocketsControl[0] = rockets[0].GetComponentInChildren<MissileControlSpiral>();
        rocketsControl[0].thrusterLeftForces = mostFit.thrusterLeftForces;
        rocketsControl[0].thrusterRightForces = mostFit.thrusterRightForces;
        rocketsControl[0].goalTransform = goalTransform;
        rocketsControl[0].speed = speed;
        rocketsControl[0].mileStones = mileStones;
        rocketsControl[0].numGenes = numGenes;
        rocketsControl[0].slerpRate = slerpRate;
        rocketsControl[0].m48 = m48;
        Destroy(rockets[1]);
        rockets[1] = Instantiate(rocketPrefab, startPos.position, rotation) as GameObject;
        rocketsControl[1] = rockets[1].GetComponentInChildren<MissileControlSpiral>();
        rocketsControl[1].thrusterLeftForces = mostFit.thrusterLeftForces;
        rocketsControl[1].thrusterRightForces = mostFit.thrusterRightForces;
        rocketsControl[1].goalTransform = goalTransform;
        rocketsControl[1].speed = speed;
        rocketsControl[1].mileStones = mileStones;
        rocketsControl[1].numGenes = numGenes;
        rocketsControl[1].slerpRate = slerpRate;
        rocketsControl[1].m48 = m48;
        for (int i = 2; i < numRockets; i++) {
            int parent1 = UnityEngine.Random.Range(0, matingpool.Count);
            int parent2 = UnityEngine.Random.Range(0, matingpool.Count);
            float[][] forces = mate(matingpool[parent1], matingpool[parent2]);
            Destroy(rockets[i]);
            rockets[i] = Instantiate(rocketPrefab, startPos.position, rotation) as GameObject;
            rocketsControl[i] = rockets[i].GetComponentInChildren<MissileControlSpiral>();
            rocketsControl[i].thrusterLeftForces = forces[0];
            rocketsControl[i].thrusterRightForces = forces[1];
            rocketsControl[i].goalTransform = goalTransform;
            rocketsControl[i].speed = speed;
            rocketsControl[i].mileStones = mileStones;
            rocketsControl[i].numGenes = numGenes;
            rocketsControl[i].slerpRate = slerpRate;
            rocketsControl[i].m48 = m48;
        }
        for (int i = 0; i < numRockets; i++) {
            rocketsControl[i].isReady = true;
        }
    }
    void mutationRange() {
        int numPassed = 0;
        int currentAvg = 0;
        int numHitTarget = 0;
        if (currentMilestoneLevel < numMilestones) {
            if (!passedMilestone[currentMilestoneLevel]) {
                for (int i = 0; i < rocketsControl.Length; i++) {
                    if (rocketsControl[i].passedMileStones[currentMilestoneLevel]) {
                        currentAvg += rocketsControl[i].currentAtMilestone[currentMilestoneLevel];
                        numPassed++;
                    }
                    if (rocketsControl[i].reachedGoal) {
                        numHitTarget++;
                    }
                }
            }
        }
        if (((float)numPassed / (float)rocketsControl.Length) > .2) {
            currentMilestoneLevel++;
            if (currentRange != numGenes - 1) {
                prevRange = currentRange;
            }
            if (!((currentAvg / (numPassed)) - 10 < 0)) {
                currentRange = (currentAvg / (numPassed)) - 10;
            }
        }
        if (((float)numHitTarget / (float)rocketsControl.Length) > .2) {
            currentRange = numGenes - 1;
        } else if (currentRange == numGenes - 1) {
            currentRange = prevRange;
        }
    }
    float[] mutate(float[] gene) {
        if (UnityEngine.Random.Range(0, 20) < mutationRate   && currentMilestoneLevel < 70) {
            int r = UnityEngine.Random.Range(5, 15);
            int end = currentRange + 50;
            if (end > numGenes - 1) {
                end = numGenes - 1;
            }
            for (int i = 0; i < r; i++) {
                gene[UnityEngine.Random.Range(currentRange, end)] = UnityEngine.Random.Range(0f, geneRange);
            }
        }
        return gene;
    }
    float[][] mate(MissileControlSpiral parent1, MissileControlSpiral parent2) {
        float[] childThrusterLeft = new float[numGenes];
        float[] childThrusterRight = new float[numGenes];
        float parent1Percentage = (float)parent1.fitness / (float)(parent1.fitness + parent2.fitness);
        float parent2Percentage = (float)parent2.fitness / (float)(parent1.fitness + parent2.fitness);
        for (int i = 0; i < numGenes; i++) {
            childThrusterLeft[i] = (parent1.thrusterLeftForces[i] * parent1Percentage) + (parent2.thrusterLeftForces[i] * parent2Percentage);
            childThrusterRight[i] = (parent1.thrusterRightForces[i] * parent1Percentage) + (parent2.thrusterRightForces[i] * parent2Percentage);
        }
        return new float[][] { mutate(childThrusterLeft), mutate(childThrusterRight) };
    }

    public void AdjustSpeed(float newSpeed) {
        speed = newSpeed;
    }
    public void AdjustMutations(float newMut) {
        mutationRate = (int)newMut;
    }
    public void AdjustPop(float newPop) {
        numRockets = (int)newPop;
    }
    public void AdjustSlerp(float newSlerp) {
        slerpRate = (int)newSlerp;
    }
    public void toggleStartSim() {
        startSimulationBool = true;
    }
}

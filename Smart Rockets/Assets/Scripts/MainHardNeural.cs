using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MainHardNeural : MonoBehaviour {
    // Start is called before the first frame update
    public int numRockets;
    public GameObject rocketPrefab;
    private GameObject[] rockets;
    private MissileControlHardGeneticNeuralNet[] rocketsControl;
    public float speed;
    public Transform goalTransform;
    private int numGenes = 100;
    private float geneRange = 1f;
    public Transform startPos;
    public Transform[] mileStones = new Transform[34];
    private bool[] passedMilestone = new bool[34];
    public int currentMilestoneLevel = 0;
    public int currentGen = 0;
    private int currentRange = 0;
    private int prevRange = 0;
    private int numMilestones = 34;
    public bool finishedTraining;
    public float slerpRate;
    public int mutationRate;
    private bool SetUpRockets = true;
    private GenerationDisplayHard generationDisplay;

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
            if (child.tag == "Canvas") {
                foreach (Transform grandChild in child) {
                    if (grandChild.tag == "Text") {
                        generationDisplay = grandChild.gameObject.GetComponent<GenerationDisplayHard>();
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
    float[,,] createWeights() {
        float[,,] weights = new float[5, 9, 9];
        for (int i = 0; i < 5; i++) {
            for (int j = 0; j < 9; j++) {
                for (int k = 0; k < 9; k++) {
                    weights[i, j, k] = UnityEngine.Random.Range(-geneRange, geneRange);
                }
            }
        }
        return weights;
    }
    float[] createLastWeights() {
        float[] weights = new float[9];
        for (int i = 0; i < 9; i++) {
            weights[i] = UnityEngine.Random.Range(-geneRange, geneRange);
        }
        return weights;
    }
    // Update is called once per frame
    void Update() {
        if (startSimulationBool == true) {
            slider1.SetActive(false);
            slider2.SetActive(false);
            slider3.SetActive(false);
            slider4.SetActive(false);
            button1.SetActive(false);
            if (SetUpRockets) {
                rockets = new GameObject[numRockets];
                rocketsControl = new MissileControlHardGeneticNeuralNet[numRockets];
                Quaternion rotation = new Quaternion(0, 0, 0, 1);
                for (int i = 0; i < numRockets; i++) {
                    rockets[i] = Instantiate(rocketPrefab, startPos.position, rotation) as GameObject;
                    rocketsControl[i] = rockets[i].GetComponentInChildren<MissileControlHardGeneticNeuralNet>();
                    rocketsControl[i].isReady = true;
                    rocketsControl[i].weights = createWeights();
                    rocketsControl[i].lastWeights = createLastWeights();
                    rocketsControl[i].goalTransform = goalTransform;
                    rocketsControl[i].mileStones = mileStones;
                    rocketsControl[i].speed = speed;
                    rocketsControl[i].numGenes = numGenes;
                    rocketsControl[i].slerpRate = slerpRate;
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
                                float[,,] weights = rocketsControl[i].weights;
                                float[] lastWeights = rocketsControl[i].lastWeights;
                                Destroy(rockets[i]);
                                rockets[i] = Instantiate(rocketPrefab, startPos.position, rotation) as GameObject;
                                rocketsControl[i] = rockets[i].GetComponentInChildren<MissileControlHardGeneticNeuralNet>();
                                rocketsControl[i].weights = weights;
                                rocketsControl[i].lastWeights = lastWeights;
                                rocketsControl[i].goalTransform = goalTransform;
                                rocketsControl[i].speed = speed;
                                rocketsControl[i].mileStones = mileStones;
                                rocketsControl[i].numGenes = numGenes;
                                rocketsControl[i].slerpRate = slerpRate;
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
                            List<MissileControlHardGeneticNeuralNet> matingpool = new List<MissileControlHardGeneticNeuralNet>();
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
    float percentFinished(MissileControlHardGeneticNeuralNet[] mce) {
        int numReachedGoal = 0;
        for (int i = 0; i < mce.Length; i++) {
            if (mce[i].reachedGoal) {
                numReachedGoal++;
            }
        }
        return (float)numReachedGoal / (float)numRockets;
    }
    bool finished(MissileControlHardGeneticNeuralNet[] rc) {
        bool finished = true;
        for (int i = 0; i < numRockets; i++) {
            finished = finished && rc[i].finished;
            if (!finished) {
                return finished;
            }
        }
        return finished;
    }
    void destroyAndCreate(List<MissileControlHardGeneticNeuralNet> matingpool, MissileControlHardGeneticNeuralNet mostFit) {
        Quaternion rotation = new Quaternion(0, 0, 0, 1);
        Destroy(rockets[0]);
        rockets[0] = Instantiate(rocketPrefab, startPos.position, rotation) as GameObject;
        rocketsControl[0] = rockets[0].GetComponentInChildren<MissileControlHardGeneticNeuralNet>();
        rocketsControl[0].weights = mostFit.weights;
        rocketsControl[0].lastWeights = mostFit.lastWeights;
        rocketsControl[0].goalTransform = goalTransform;
        rocketsControl[0].speed = speed;
        rocketsControl[0].mileStones = mileStones;
        rocketsControl[0].numGenes = numGenes;
        rocketsControl[0].slerpRate = slerpRate;
        Destroy(rockets[1]);
        rockets[1] = Instantiate(rocketPrefab, startPos.position, rotation) as GameObject;
        rocketsControl[1] = rockets[1].GetComponentInChildren<MissileControlHardGeneticNeuralNet>();
        rocketsControl[1].weights = mostFit.weights;
        rocketsControl[1].lastWeights = mostFit.lastWeights;
        rocketsControl[1].goalTransform = goalTransform;
        rocketsControl[1].speed = speed;
        rocketsControl[1].mileStones = mileStones;
        rocketsControl[1].numGenes = numGenes;
        rocketsControl[1].slerpRate = slerpRate;
        for (int i = 2; i < numRockets; i++) {
            int parent1 = UnityEngine.Random.Range(0, matingpool.Count);
            int parent2 = UnityEngine.Random.Range(0, matingpool.Count);
            float[,,] weights = mate(matingpool[parent1], matingpool[parent2]);
            float[] lastWeights = mateLastWeights(matingpool[parent1], matingpool[parent2]);
            Destroy(rockets[i]);
            rockets[i] = Instantiate(rocketPrefab, startPos.position, rotation) as GameObject;
            rocketsControl[i] = rockets[i].GetComponentInChildren<MissileControlHardGeneticNeuralNet>();
            rocketsControl[i].weights = weights;
            rocketsControl[i].lastWeights = lastWeights;
            rocketsControl[i].goalTransform = goalTransform;
            rocketsControl[i].speed = speed;
            rocketsControl[i].mileStones = mileStones;
            rocketsControl[i].numGenes = numGenes;
            rocketsControl[i].slerpRate = slerpRate;
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
            if (!((currentAvg / (numPassed)) - 8 < 0)) {
                currentRange = (currentAvg / (numPassed)) - 8;
            }
        }
        if (((float)numHitTarget / (float)rocketsControl.Length) > .2) {
            currentRange = numGenes - 1;
        } else if (currentRange == numGenes - 1) {
            currentRange = prevRange;
        }
    }
    float[,,] mutate(float[,,] weights) {
        if (UnityEngine.Random.Range(0, 20) < mutationRate) {
            int r = UnityEngine.Random.Range(5, 20);
            for (int i = 0; i < r; i++) {
                weights[UnityEngine.Random.Range(0, 4), UnityEngine.Random.Range(0, 9), UnityEngine.Random.Range(0, 9)] = UnityEngine.Random.Range(-geneRange, geneRange);
            }
        }
        return weights;
    }
    float[] mutateLastWeights(float[] weights) {
        if (UnityEngine.Random.Range(0, 20) < mutationRate / 2) {
            int r = UnityEngine.Random.Range(0, 2);
            for (int i = 0; i < r; i++) {
                weights[UnityEngine.Random.Range(0, 9)] = UnityEngine.Random.Range(-geneRange, geneRange);
            }
        }
        return weights;
    }
    float[,,] mate(MissileControlHardGeneticNeuralNet parent1, MissileControlHardGeneticNeuralNet parent2) {
        float[,,] childWeights = new float[5, 9, 9];
        float parent1Percentage = (float)parent1.fitness / (float)(parent1.fitness + parent2.fitness);
        float parent2Percentage = (float)parent2.fitness / (float)(parent1.fitness + parent2.fitness);
        for (int i = 0; i < 5; i++) {
            for (int j = 0; j < 9; j++) {
                for (int k = 0; k < 9; k++) {
                    childWeights[i, j, k] = (parent1.weights[i, j, k] * parent1Percentage) + (parent2.weights[i, j, k] * parent2Percentage);
                }
            }
        }
        return mutate(childWeights);
    }
    float[] mateLastWeights(MissileControlHardGeneticNeuralNet parent1, MissileControlHardGeneticNeuralNet parent2) {
        float[] childWeights = new float[9];
        float parent1Percentage = (float)parent1.fitness / (float)(parent1.fitness + parent2.fitness);
        float parent2Percentage = (float)parent2.fitness / (float)(parent1.fitness + parent2.fitness);
        for (int i = 0; i < 9; i++) {
            childWeights[i] = (parent1.lastWeights[i] * parent1Percentage) + (parent2.lastWeights[i] * parent2Percentage);
        }
        return mutateLastWeights(childWeights);
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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MainHardBezier : MonoBehaviour {
    // Start is called before the first frame update
    public int numRockets;
    public GameObject rocketPrefab;
    private GameObject[] rockets;
    private MissileControlHardBezier[] rocketsControl;
    public float speed;
    public Transform goalTransform;
    private int numGenes = 13;
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

    public Vector3[] desiredPath;

    public bool startSimulationBool = false;
    private GameObject slider1;
    private GameObject slider2;
    private GameObject slider3;
    private GameObject slider4;
    private GameObject button1;

    void Start() {
        slerpRate = 5f;
        mutationRate = 1;
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
        desiredPath = new Vector3[] {
            startPos.position,
            new Vector3(10.4f,14.7f,0),
            new Vector3(8.99f,17.44f,0),
            new Vector3(6.6f,19.22f,0),
            new Vector3(2.37f,19.23f,0),
            new Vector3(-1.1f,15.74f,0),
            new Vector3(-1.1f,.25f,0),
            new Vector3(-3.94f,-1.96f,0),
            new Vector3(-8.46f,-1.41f,0),
            new Vector3(-9.69f,-0.07f,0),
            new Vector3(-10.07f,19.72f,0),
            new Vector3(-7.23f,22.78f,0),
            new Vector3(-4.33f,25.09f,0),
            goalTransform.position

        };
    }
    Vector3[] createPath() {
        Vector3[] path = new Vector3[numGenes];
        for (int i = 0; i < numGenes; i++) {
            if (i == 0) {
                path[i] = startPos.position;
            } else if (i == numGenes - 1) {
                path[i] = goalTransform.position;
            } else {
                path[i] = new Vector3(UnityEngine.Random.Range(-12f, 12f), UnityEngine.Random.Range(-4f, 27f), 0);
            }
        }
        return path;
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
                rocketsControl = new MissileControlHardBezier[numRockets];
                Quaternion rotation = new Quaternion(0, 0, 0, 1);
                for (int i = 0; i < numRockets; i++) {
                    rockets[i] = Instantiate(rocketPrefab, startPos.position, rotation) as GameObject;
                    rocketsControl[i] = rockets[i].GetComponentInChildren<MissileControlHardBezier>();
                    rocketsControl[i].isReady = true;
                    rocketsControl[i].path = createPath();
                    rocketsControl[i].goalTransform = goalTransform;
                    rocketsControl[i].mileStones = mileStones;
                    rocketsControl[i].speed = speed;
                    rocketsControl[i].numGenes = numGenes;
                    rocketsControl[i].slerpRate = slerpRate;
                    rocketsControl[i].desiredPath = desiredPath;
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
                        //if (samePos) {
                            if(false) {
                            Quaternion rotation = new Quaternion(0, 0, 0, 1);
                            for (int i = 0; i < numRockets; i++) {
                                Vector3[] path = rocketsControl[i].path;
                                Destroy(rockets[i]);
                                rockets[i] = Instantiate(rocketPrefab, startPos.position, rotation) as GameObject;
                                rocketsControl[i] = rockets[i].GetComponentInChildren<MissileControlHardBezier>();
                                rocketsControl[i].path = path;
                                rocketsControl[i].goalTransform = goalTransform;
                                rocketsControl[i].speed = speed;
                                rocketsControl[i].mileStones = mileStones;
                                rocketsControl[i].numGenes = numGenes;
                                rocketsControl[i].slerpRate = slerpRate;
                                rocketsControl[i].desiredPath = desiredPath;
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
                            List<MissileControlHardBezier> matingpool = new List<MissileControlHardBezier>();
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
    float percentFinished(MissileControlHardBezier[] mce) {
        int numReachedGoal = 0;
        for (int i = 0; i < mce.Length; i++) {
            if (mce[i].reachedGoal) {
                numReachedGoal++;
            }
        }
        return (float)numReachedGoal / (float)numRockets;
    }
    bool finished(MissileControlHardBezier[] rc) {
        bool finished = true;
        for (int i = 0; i < numRockets; i++) {
            finished = finished && rc[i].finished;
            if (!finished) {
                return finished;
            }
        }
        return finished;
    }
    void destroyAndCreate(List<MissileControlHardBezier> matingpool, MissileControlHardBezier mostFit) {
        Quaternion rotation = new Quaternion(0, 0, 0, 1);
        Destroy(rockets[0]);
        rockets[0] = Instantiate(rocketPrefab, startPos.position, rotation) as GameObject;
        rocketsControl[0] = rockets[0].GetComponentInChildren<MissileControlHardBezier>();
        rocketsControl[0].path = mostFit.path;
        rocketsControl[0].goalTransform = goalTransform;
        rocketsControl[0].speed = speed;
        rocketsControl[0].mileStones = mileStones;
        rocketsControl[0].numGenes = numGenes;
        rocketsControl[0].slerpRate = slerpRate;
        rocketsControl[0].desiredPath = desiredPath;
        Destroy(rockets[1]);
        rockets[1] = Instantiate(rocketPrefab, startPos.position, rotation) as GameObject;
        rocketsControl[1] = rockets[1].GetComponentInChildren<MissileControlHardBezier>();
        rocketsControl[1].path = mostFit.path;
        rocketsControl[1].goalTransform = goalTransform;
        rocketsControl[1].speed = speed;
        rocketsControl[1].mileStones = mileStones;
        rocketsControl[1].numGenes = numGenes;
        rocketsControl[1].slerpRate = slerpRate;
        rocketsControl[1].desiredPath = desiredPath;
        for (int i = 2; i < numRockets; i++) {
            int parent1 = UnityEngine.Random.Range(0, matingpool.Count);
            int parent2 = UnityEngine.Random.Range(0, matingpool.Count);
            Vector3[] path = mate(matingpool[parent1], matingpool[parent2]);
            Destroy(rockets[i]);
            rockets[i] = Instantiate(rocketPrefab, startPos.position, rotation) as GameObject;
            rocketsControl[i] = rockets[i].GetComponentInChildren<MissileControlHardBezier>();
            rocketsControl[i].path = path;
            rocketsControl[i].goalTransform = goalTransform;
            rocketsControl[i].speed = speed;
            rocketsControl[i].mileStones = mileStones;
            rocketsControl[i].numGenes = numGenes;
            rocketsControl[i].slerpRate = slerpRate;
            rocketsControl[i].desiredPath = desiredPath;
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
    Vector3[] mutate(Vector3[] path) {
        if (UnityEngine.Random.Range(0, 20) < mutationRate) {
            int r = UnityEngine.Random.Range(1, 3);
            for (int i = 0; i < r; i++) {
                int c = UnityEngine.Random.Range(1, 7);
                float x = UnityEngine.Random.Range(-4f, 4f);
                float y = UnityEngine.Random.Range(-4f, 4f);
                Vector3 n = new Vector3(x, y, 0) + path[c];
                if (n.x > 12) {
                    n.x = 12;
                } else if (n.x < -12) {
                    n.x = -12;
                }
                if (n.y > 27) {
                    n.y = 27;
                } else if (n.y < -4) {
                    n.y = -4;
                }
                path[c] = n;
            }
        }
        return path;
    }
    Vector3[] mate(MissileControlHardBezier parent1, MissileControlHardBezier parent2) {
        Vector3[] childPath = new Vector3[numGenes];
        float parent1Percentage = (float)parent1.fitness / (float)(parent1.fitness + parent2.fitness);
        float parent2Percentage = (float)parent2.fitness / (float)(parent1.fitness + parent2.fitness);
        Vector3 childPoint;
        for (int i = 0; i < childPath.Length; i++) {
            childPoint = parent1Percentage * parent1.path[i] + parent2Percentage * parent2.path[i];
            childPath[i] = childPoint;
        }
        return mutate(childPath);
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
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class MissileControlHardGeneticNeuralNet : MonoBehaviour {
    // Start is called before the first frame update
    [SerializeField]
    public float speed;
    public float slerpRate;
    private int count;
    Rigidbody2D rb;
    public bool isReady;
    public float[,,] weights;
    public float[] lastWeights;
    public int current;
    public Transform goalTransform;
    public double fitness;
    public bool finished;
    private bool crashed;
    public bool reachedGoal;
    private double maxDist;
    public Transform[] mileStones;
    public bool[] passedMileStones;
    public int[] currentAtMilestone;
    private int fitnessLevel;
    public GameObject explosion;
    private bool exploded;
    public int numGenes;
    public float[] crashPos;
    private int layerMask = ~(1 << 9);


    void Awake() {
        rb = this.gameObject.GetComponent<Rigidbody2D>();
    }

    void Start() {
        fitness = 0f;
        current = 0;
        count = 0;
        finished = false;
        maxDist = Math.Sqrt(Math.Pow((double)(transform.position.x - goalTransform.position.x), 2) +
            Math.Pow((double)(transform.position.y - goalTransform.position.y), 2));
        maxDist = 200;
        Physics2D.gravity = Vector2.zero;
        fitnessLevel = 1;
        passedMileStones = new bool[34];
        currentAtMilestone = new int[34];
        for (int i = 0; i < passedMileStones.Length; i++) {
            passedMileStones[i] = false;
        }
        exploded = false;
        crashPos = new float[2];
    }

    void OnCollisionEnter2D(Collision2D collision) {
        if (collision.gameObject.tag == "Rocket") {
            Physics2D.IgnoreCollision(collision.gameObject.GetComponent<CompositeCollider2D>(), GetComponent<CompositeCollider2D>());
            Physics2D.IgnoreCollision(collision.gameObject.GetComponent<Collider2D>(), GetComponent<Collider2D>());
            Physics2D.IgnoreCollision(collision.gameObject.GetComponent<CapsuleCollider2D>(), GetComponent<CapsuleCollider2D>());
        }
        if (collision.gameObject.tag == "wall" && !reachedGoal) { //prevents it from updating fitness after it has finished the stage
            crashPos[0] = transform.position.x;
            crashPos[1] = transform.position.y;
            fitness *= .50;
            crashed = true;
            rb.freezeRotation = true;
            GetComponent<MeshRenderer>().enabled = false;
            if (!exploded) {
                exploded = true;
                ParticleSystem[] ps = GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem child in ps) {
                    child.Stop();
                    child.Clear();
                }
                GameObject expl = Instantiate(explosion, transform.position, Quaternion.identity) as GameObject;
                Destroy(expl, 1);
            }
        }
        if (collision.gameObject.tag == "Goal" && !crashed) {
            fitness *= 4;
            reachedGoal = true;
            crashed = true;
            GetComponent<MeshRenderer>().enabled = false;
            if (!exploded) {
                exploded = true;
                ParticleSystem[] ps = GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem child in ps) {
                    child.Stop();
                    child.Clear();
                }
                GameObject expl = Instantiate(explosion, transform.position, Quaternion.identity) as GameObject;
                Destroy(expl, 1);
            }
        }
    }
    float[] normalize(float[] array) {
        float max = array.Max();
        float[] nArray = new float[array.Length];
        for (int i = 0; i < nArray.Length; i++) {
            nArray[i] = array[i] / max;
        }
        return nArray;
    }
    double getAngle(float north, float west, float east, float northWest, float northNorthWest, float northWestWest, float northEast, float northNorthEast, float northEastEast) {
        float[] normalizedArray = normalize(new float[] { north, west, east, northWest, northNorthWest, northWestWest, northEast, northNorthEast, northEastEast });
        float[] values = new float[9];
        for (int i = 0; i < 1; i++) {
            //layer
            for (int j = 0; j < 9; j++) {
                //node
                float value = 0;
                for (int k = 0; k < 9; k++) {
                    //weight of node
                    value += weights[i, j, k] * normalizedArray[k];
                }
                values[j] = (float)Math.Tanh(value);
            }
            normalizedArray = values;
            values = new float[9];
        }
        float lastValue = 0;
        for (int k = 0; k < 9; k++) {
            lastValue += lastWeights[k] * normalizedArray[k];
        }
        return Math.Tanh(lastValue) * 180;
    }
    // Update is called once per frame
    void Update() {
        RaycastHit2D north = Physics2D.Raycast(transform.position, transform.up, 50, layerMask);
        RaycastHit2D west = Physics2D.Raycast(transform.position, Quaternion.Euler(0, 0, 90) * transform.up);
        RaycastHit2D northWest = Physics2D.Raycast(transform.position, Quaternion.Euler(0, 0, 45) * transform.up);
        RaycastHit2D northNorthWest = Physics2D.Raycast(transform.position, Quaternion.Euler(0, 0, 22.5f) * transform.up);
        RaycastHit2D northWestWest = Physics2D.Raycast(transform.position, Quaternion.Euler(0, 0, 67.5f) * transform.up);
        RaycastHit2D east = Physics2D.Raycast(transform.position, Quaternion.Euler(0, 0, -90) * transform.up);
        RaycastHit2D northEast = Physics2D.Raycast(transform.position, Quaternion.Euler(0, 0, -45) * transform.up);
        RaycastHit2D northNorthEast = Physics2D.Raycast(transform.position, Quaternion.Euler(0, 0, -22.5f) * transform.up);
        RaycastHit2D northEastEast = Physics2D.Raycast(transform.position, Quaternion.Euler(0, 0, -67.5f) * transform.up);

        if (isReady && !crashed) {
            rb.velocity = transform.up * speed;
            if (current < numGenes && count % 5 == 0) { //change range of forces applied on rockets || remove count %10? 
                current++;  //this runs 50 times in total
            }
            count++;
        }
        if (current < numGenes) {
            double angle = getAngle(north.distance, west.distance, east.distance, northWest.distance, northNorthWest.distance, northWestWest.distance, northEast.distance, northNorthEast.distance, northEastEast.distance);
            Quaternion target = Quaternion.Euler(0, 0, transform.eulerAngles.z + (float)angle);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * slerpRate);
        }
        if (crashed || current >= numGenes) {
            finished = true;
        }
        if (!finished) {
            calculateFitness();
        }
    }
    void getFitnessLevel() {
        if (!passedMileStones[0]
            && mileStones[0].position.y < transform.position.y) {
            passedMileStones[0] = true;
            fitnessLevel = 2;
            if (mileStones[0].position.x < transform.position.x
            && (mileStones[0].position.x + 1) > transform.position.x) {
                fitnessLevel += 1;
            }
            fitness += fitnessLevel;
            currentAtMilestone[0] = current;
        } else if (passedMileStones[0]
            && !passedMileStones[1]
            && mileStones[1].position.y < transform.position.y) {
            passedMileStones[1] = true;
            fitnessLevel = 3;
            if (mileStones[1].position.x < transform.position.x
            && (mileStones[1].position.x + 1) > transform.position.x) {
                fitnessLevel += 1;
            }
            fitness += fitnessLevel;
            currentAtMilestone[1] = current;
        } else if (passedMileStones[0]
            && passedMileStones[1]
            && !passedMileStones[2]
            && mileStones[2].position.y < transform.position.y) {
            passedMileStones[2] = true;
            fitnessLevel = 4;
            if (mileStones[2].position.x < transform.position.x
            && (mileStones[2].position.x + 1) > transform.position.x) {
                fitnessLevel += 2;
            }
            fitness += fitnessLevel;
            currentAtMilestone[2] = current;
        } else if (passedMileStones[0]
            && passedMileStones[1]
            && passedMileStones[2]
            && !passedMileStones[3]
            && mileStones[3].position.y < transform.position.y) {
            passedMileStones[3] = true;
            fitnessLevel = 5;
            if (mileStones[3].position.x < transform.position.x
            && (mileStones[3].position.x + 1) > transform.position.x) {
                fitnessLevel += 2;
            }
            fitness += fitnessLevel;
            currentAtMilestone[3] = current;
        } else if (passedMileStones[0]
            && passedMileStones[1]
            && passedMileStones[2]
            && passedMileStones[3]
            && !passedMileStones[4]
            && mileStones[4].position.y < transform.position.y) {
            passedMileStones[4] = true;
            fitnessLevel = 6;
            if (mileStones[4].position.x < transform.position.x
            && (mileStones[4].position.x + 1) > transform.position.x) {
                fitnessLevel += 3;
            }
            fitness += fitnessLevel;
            currentAtMilestone[4] = current;
        } else if (passedMileStones[0]
            && passedMileStones[1]
            && passedMileStones[2]
            && passedMileStones[3]
            && passedMileStones[4]
            && !passedMileStones[5]
            && mileStones[5].position.y < transform.position.y) {
            passedMileStones[5] = true;
            fitnessLevel = 7;
            if (mileStones[5].position.x < transform.position.x
            && (mileStones[5].position.x + 1) > transform.position.x) {
                fitnessLevel += 3;
            }
            fitness += fitnessLevel;
            currentAtMilestone[5] = current;
        } else if (passedMileStones[0]
            && passedMileStones[1]
            && passedMileStones[2]
            && passedMileStones[3]
            && passedMileStones[4]
            && passedMileStones[5]
            && !passedMileStones[6]
            && mileStones[6].position.x > transform.position.x) {
            passedMileStones[6] = true;
            fitnessLevel = 8;
            if (mileStones[6].position.y < transform.position.y
            && (mileStones[6].position.y + 1.25) > transform.position.y) {
                fitnessLevel += 4;
            }
            fitness += fitnessLevel;
            currentAtMilestone[6] = current;
        } else if (passedMileStones[0]
            && passedMileStones[1]
            && passedMileStones[2]
            && passedMileStones[3]
            && passedMileStones[4]
            && passedMileStones[5]
            && passedMileStones[6]
            && !passedMileStones[7]
            && mileStones[7].position.x > transform.position.x) {
            passedMileStones[7] = true;
            fitnessLevel = 9;
            if (mileStones[7].position.y < transform.position.y
            && (mileStones[7].position.y + 1.15) > transform.position.y) {
                fitnessLevel += 4;
            }
            fitness += fitnessLevel;
            currentAtMilestone[7] = current;
        } else if (passedMileStones[0]
            && passedMileStones[1]
            && passedMileStones[2]
            && passedMileStones[3]
            && passedMileStones[4]
            && passedMileStones[5]
            && passedMileStones[6]
            && passedMileStones[7]
            && !passedMileStones[8]
            && mileStones[8].position.x > transform.position.x) {
            passedMileStones[8] = true;
            fitnessLevel = 10;
            if (mileStones[8].position.y < transform.position.y
            && (mileStones[8].position.y + 1.05) > transform.position.y) {
                fitnessLevel += 4;
            }
            fitness += fitnessLevel;
            currentAtMilestone[8] = current;
        } else if (passedMileStones[0]
            && passedMileStones[1]
            && passedMileStones[2]
            && passedMileStones[3]
            && passedMileStones[4]
            && passedMileStones[5]
            && passedMileStones[6]
            && passedMileStones[7]
            && passedMileStones[8]
            && !passedMileStones[9]
            && (mileStones[9].position.x + 1) > transform.position.x) {
            passedMileStones[9] = true;
            if (mileStones[9].position.y > transform.position.y
            && (mileStones[9].position.y + 1) < transform.position.y) {
                fitnessLevel += 5;
            }
            fitness += fitnessLevel;
            fitnessLevel = 11;
            currentAtMilestone[9] = current;
        } else if (passedMileStones[0]
            && passedMileStones[1]
            && passedMileStones[2]
            && passedMileStones[3]
            && passedMileStones[4]
            && passedMileStones[5]
            && passedMileStones[6]
            && passedMileStones[7]
            && passedMileStones[8]
            && passedMileStones[9]
            && !passedMileStones[10]
            && mileStones[10].position.y > transform.position.y) {
            passedMileStones[10] = true;
            fitnessLevel = 15;
            if (mileStones[10].position.x < transform.position.x
            && (mileStones[10].position.x + 2) > transform.position.x) {
                fitnessLevel += 5;
            }
            fitness += fitnessLevel;
            currentAtMilestone[10] = current;
        } else if (passedMileStones[0]
            && passedMileStones[1]
            && passedMileStones[2]
            && passedMileStones[3]
            && passedMileStones[4]
            && passedMileStones[5]
            && passedMileStones[6]
            && passedMileStones[7]
            && passedMileStones[8]
            && passedMileStones[9]
            && passedMileStones[10]
            && !passedMileStones[11]
            && mileStones[11].position.y > transform.position.y) {
            passedMileStones[11] = true;
            fitnessLevel = 18;
            if (mileStones[11].position.x < transform.position.x
            && (mileStones[11].position.x + 1) > transform.position.x) {
                fitnessLevel += 5;
            }
            fitness += fitnessLevel;
            currentAtMilestone[11] = current;
        } else if (passedMileStones[0]
            && passedMileStones[1]
            && passedMileStones[2]
            && passedMileStones[3]
            && passedMileStones[4]
            && passedMileStones[5]
            && passedMileStones[6]
            && passedMileStones[7]
            && passedMileStones[8]
            && passedMileStones[9]
            && passedMileStones[10]
            && passedMileStones[11]
            && !passedMileStones[12]
            && mileStones[12].position.y > transform.position.y) {
            passedMileStones[12] = true;
            fitnessLevel = 19;
            if (mileStones[12].position.x < transform.position.x
            && (mileStones[12].position.x + 1) > transform.position.x) {
                fitnessLevel += 6;
            }
            fitness += fitnessLevel;
            currentAtMilestone[12] = current;
        } else if (passedMileStones[0]
            && passedMileStones[1]
            && passedMileStones[2]
            && passedMileStones[3]
            && passedMileStones[4]
            && passedMileStones[5]
            && passedMileStones[6]
            && passedMileStones[7]
            && passedMileStones[8]
            && passedMileStones[9]
            && passedMileStones[10]
            && passedMileStones[11]
            && passedMileStones[12]
            && !passedMileStones[13]
            && mileStones[13].position.y > transform.position.y) {
            passedMileStones[13] = true;
            fitnessLevel = 22;
            if (mileStones[13].position.x < transform.position.x
            && (mileStones[13].position.x + 1) > transform.position.x) {
                fitnessLevel += 6;
            }
            fitness += fitnessLevel;
            currentAtMilestone[13] = current;
        } else if (passedMileStones[0]
            && passedMileStones[1]
            && passedMileStones[2]
            && passedMileStones[3]
            && passedMileStones[4]
            && passedMileStones[5]
            && passedMileStones[6]
            && passedMileStones[7]
            && passedMileStones[8]
            && passedMileStones[9]
            && passedMileStones[10]
            && passedMileStones[11]
            && passedMileStones[12]
            && passedMileStones[13]
            && !passedMileStones[14]
            && mileStones[14].position.y > transform.position.y) {
            passedMileStones[14] = true;
            fitnessLevel = 23;
            if (mileStones[14].position.x < transform.position.x
            && (mileStones[14].position.x + 1) > transform.position.x) {
                fitnessLevel += 7;
            }
            fitness += fitnessLevel;
            currentAtMilestone[14] = current;
        } else if (passedMileStones[0]
            && passedMileStones[1]
            && passedMileStones[2]
            && passedMileStones[3]
            && passedMileStones[4]
            && passedMileStones[5]
            && passedMileStones[6]
            && passedMileStones[7]
            && passedMileStones[8]
            && passedMileStones[9]
            && passedMileStones[10]
            && passedMileStones[11]
            && passedMileStones[12]
            && passedMileStones[13]
            && passedMileStones[14]
            && !passedMileStones[15]
            && mileStones[15].position.y > transform.position.y) {
            passedMileStones[15] = true;
            fitnessLevel = 24;
            if (mileStones[15].position.x < transform.position.x
            && (mileStones[15].position.x + 1) > transform.position.x) {
                fitnessLevel += 7;
            }
            fitness += fitnessLevel;
            currentAtMilestone[15] = current;
        } else if (passedMileStones[0]
            && passedMileStones[1]
            && passedMileStones[2]
            && passedMileStones[3]
            && passedMileStones[4]
            && passedMileStones[5]
            && passedMileStones[6]
            && passedMileStones[7]
            && passedMileStones[8]
            && passedMileStones[9]
            && passedMileStones[10]
            && passedMileStones[11]
            && passedMileStones[12]
            && passedMileStones[13]
            && passedMileStones[14]
            && passedMileStones[15]
            && !passedMileStones[16]
            && mileStones[16].position.y > transform.position.y) {
            passedMileStones[16] = true;
            fitnessLevel = 27;
            if (mileStones[16].position.x < transform.position.x
            && (mileStones[16].position.x + 1) > transform.position.x) {
                fitnessLevel += 8;
            }
            fitness += fitnessLevel;
            currentAtMilestone[16] = current;
        } else if (passedMileStones[0]
             && passedMileStones[1]
             && passedMileStones[2]
             && passedMileStones[3]
             && passedMileStones[4]
             && passedMileStones[5]
             && passedMileStones[6]
             && passedMileStones[7]
             && passedMileStones[8]
             && passedMileStones[9]
             && passedMileStones[10]
             && passedMileStones[11]
             && passedMileStones[12]
             && passedMileStones[13]
             && passedMileStones[14]
             && passedMileStones[15]
             && passedMileStones[16]
             && !passedMileStones[17]
             && mileStones[17].position.y > transform.position.y) {
            passedMileStones[17] = true;
            fitnessLevel = 35;
            if (mileStones[17].position.x < transform.position.x
            && (mileStones[17].position.x + 1) > transform.position.x) {
                fitnessLevel += 10;
            }
            fitness += fitnessLevel;
            currentAtMilestone[17] = current;
        } else if (passedMileStones[0]
              && passedMileStones[1]
              && passedMileStones[2]
              && passedMileStones[3]
              && passedMileStones[4]
              && passedMileStones[5]
              && passedMileStones[6]
              && passedMileStones[7]
              && passedMileStones[8]
              && passedMileStones[9]
              && passedMileStones[10]
              && passedMileStones[11]
              && passedMileStones[12]
              && passedMileStones[13]
              && passedMileStones[14]
              && passedMileStones[15]
              && passedMileStones[16]
              && passedMileStones[17]
              && !passedMileStones[18]
              && mileStones[18].position.y > transform.position.y) {
            passedMileStones[18] = true;
            fitnessLevel = 40;
            if (mileStones[18].position.x < transform.position.x
              && (mileStones[18].position.x + 1) > transform.position.x) {
                fitnessLevel += 13;
            }
            fitness += fitnessLevel;
            currentAtMilestone[18] = current;
        } else if (passedMileStones[0]
               && passedMileStones[1]
               && passedMileStones[2]
               && passedMileStones[3]
               && passedMileStones[4]
               && passedMileStones[5]
               && passedMileStones[6]
               && passedMileStones[7]
               && passedMileStones[8]
               && passedMileStones[9]
               && passedMileStones[10]
               && passedMileStones[11]
               && passedMileStones[12]
               && passedMileStones[13]
               && passedMileStones[14]
               && passedMileStones[15]
               && passedMileStones[16]
               && passedMileStones[17]
               && passedMileStones[18]
               && !passedMileStones[19]
               && mileStones[19].position.x > transform.position.x) {
            passedMileStones[19] = true;
            fitnessLevel = 70;
            if (mileStones[19].position.y < transform.position.y
              && (mileStones[19].position.y + 2) > transform.position.y) {
                fitnessLevel += 25;
            }
            fitness += fitnessLevel;
            currentAtMilestone[19] = current;
        } else if (passedMileStones[0]
                && passedMileStones[1]
                && passedMileStones[2]
                && passedMileStones[3]
                && passedMileStones[4]
                && passedMileStones[5]
                && passedMileStones[6]
                && passedMileStones[7]
                && passedMileStones[8]
                && passedMileStones[9]
                && passedMileStones[10]
                && passedMileStones[11]
                && passedMileStones[12]
                && passedMileStones[13]
                && passedMileStones[14]
                && passedMileStones[15]
                && passedMileStones[16]
                && passedMileStones[17]
                && passedMileStones[18]
                && passedMileStones[19]
                && !passedMileStones[20]
                && mileStones[20].position.x > transform.position.x) {
            passedMileStones[20] = true;
            fitnessLevel = 90;
            if (mileStones[20].position.y < transform.position.y
              && (mileStones[20].position.y + 2) > transform.position.y) {
                fitnessLevel += 30;
            }
            fitness += fitnessLevel;
            currentAtMilestone[20] = current;
        } else if (passedMileStones[0]
                && passedMileStones[1]
                && passedMileStones[2]
                && passedMileStones[3]
                && passedMileStones[4]
                && passedMileStones[5]
                && passedMileStones[6]
                && passedMileStones[7]
                && passedMileStones[8]
                && passedMileStones[9]
                && passedMileStones[10]
                && passedMileStones[11]
                && passedMileStones[12]
                && passedMileStones[13]
                && passedMileStones[14]
                && passedMileStones[15]
                && passedMileStones[16]
                && passedMileStones[17]
                && passedMileStones[18]
                && passedMileStones[19]
                && passedMileStones[20]
                && !passedMileStones[21]
                && mileStones[21].position.y < transform.position.y) {
            passedMileStones[21] = true;
            fitnessLevel = 70;
            if (mileStones[21].position.x < transform.position.x
              && (mileStones[21].position.x + 2) > transform.position.x) {
                fitnessLevel += 63;
            }
            fitness += fitnessLevel;
            currentAtMilestone[21] = current;
        } else if (passedMileStones[0]
                && passedMileStones[1]
                && passedMileStones[2]
                && passedMileStones[3]
                && passedMileStones[4]
                && passedMileStones[5]
                && passedMileStones[6]
                && passedMileStones[7]
                && passedMileStones[8]
                && passedMileStones[9]
                && passedMileStones[10]
                && passedMileStones[11]
                && passedMileStones[12]
                && passedMileStones[13]
                && passedMileStones[14]
                && passedMileStones[15]
                && passedMileStones[16]
                && passedMileStones[17]
                && passedMileStones[18]
                && passedMileStones[19]
                && passedMileStones[20]
                && passedMileStones[21]
                && !passedMileStones[22]
                && mileStones[22].position.y < transform.position.y) {
            passedMileStones[22] = true;
            fitnessLevel = 115;
            if (mileStones[22].position.x < transform.position.x
              && (mileStones[22].position.x + 1) > transform.position.x) {
                fitnessLevel += 65;
            }
            fitness += fitnessLevel;
            currentAtMilestone[22] = current;
        } else if (passedMileStones[0]
                && passedMileStones[1]
                && passedMileStones[2]
                && passedMileStones[3]
                && passedMileStones[4]
                && passedMileStones[5]
                && passedMileStones[6]
                && passedMileStones[7]
                && passedMileStones[8]
                && passedMileStones[9]
                && passedMileStones[10]
                && passedMileStones[11]
                && passedMileStones[12]
                && passedMileStones[13]
                && passedMileStones[14]
                && passedMileStones[15]
                && passedMileStones[16]
                && passedMileStones[17]
                && passedMileStones[18]
                && passedMileStones[19]
                && passedMileStones[20]
                && passedMileStones[21]
                && passedMileStones[22]
                && !passedMileStones[23]
                && mileStones[23].position.y < transform.position.y) {
            passedMileStones[23] = true;
            fitnessLevel = 120;
            if (mileStones[23].position.x < transform.position.x
              && (mileStones[23].position.x + 1) > transform.position.x) {
                fitnessLevel += 67;
            }
            fitness += fitnessLevel;
            currentAtMilestone[23] = current;
        } else if (passedMileStones[0]
                && passedMileStones[1]
                && passedMileStones[2]
                && passedMileStones[3]
                && passedMileStones[4]
                && passedMileStones[5]
                && passedMileStones[6]
                && passedMileStones[7]
                && passedMileStones[8]
                && passedMileStones[9]
                && passedMileStones[10]
                && passedMileStones[11]
                && passedMileStones[12]
                && passedMileStones[13]
                && passedMileStones[14]
                && passedMileStones[15]
                && passedMileStones[16]
                && passedMileStones[17]
                && passedMileStones[18]
                && passedMileStones[19]
                && passedMileStones[20]
                && passedMileStones[21]
                && passedMileStones[22]
                && passedMileStones[23]
                && !passedMileStones[24]
                && mileStones[24].position.y < transform.position.y) {
            passedMileStones[24] = true;
            fitnessLevel = 125;
            if (mileStones[24].position.x < transform.position.x
              && (mileStones[24].position.x + 1) > transform.position.x) {
                fitnessLevel += 67;
            }
            fitness += fitnessLevel;
            currentAtMilestone[24] = current;
        } else if (passedMileStones[0]
                && passedMileStones[1]
                && passedMileStones[2]
                && passedMileStones[3]
                && passedMileStones[4]
                && passedMileStones[5]
                && passedMileStones[6]
                && passedMileStones[7]
                && passedMileStones[8]
                && passedMileStones[9]
                && passedMileStones[10]
                && passedMileStones[11]
                && passedMileStones[12]
                && passedMileStones[13]
                && passedMileStones[14]
                && passedMileStones[15]
                && passedMileStones[16]
                && passedMileStones[17]
                && passedMileStones[18]
                && passedMileStones[19]
                && passedMileStones[20]
                && passedMileStones[21]
                && passedMileStones[22]
                && passedMileStones[23]
                && passedMileStones[24]
                && !passedMileStones[25]
                && mileStones[25].position.y < transform.position.y) {
            passedMileStones[25] = true;
            fitnessLevel = 130;
            if (mileStones[25].position.x < transform.position.x
              && (mileStones[25].position.x + 1) > transform.position.x) {
                fitnessLevel += 70;
            }
            fitness += fitnessLevel;
            currentAtMilestone[25] = current;
        } else if (passedMileStones[0]
                && passedMileStones[1]
                && passedMileStones[2]
                && passedMileStones[3]
                && passedMileStones[4]
                && passedMileStones[5]
                && passedMileStones[6]
                && passedMileStones[7]
                && passedMileStones[8]
                && passedMileStones[9]
                && passedMileStones[10]
                && passedMileStones[11]
                && passedMileStones[12]
                && passedMileStones[13]
                && passedMileStones[14]
                && passedMileStones[15]
                && passedMileStones[16]
                && passedMileStones[17]
                && passedMileStones[18]
                && passedMileStones[19]
                && passedMileStones[20]
                && passedMileStones[21]
                && passedMileStones[22]
                && passedMileStones[23]
                && passedMileStones[24]
                && passedMileStones[25]
                && !passedMileStones[26]
                && mileStones[26].position.y < transform.position.y) {
            passedMileStones[26] = true;
            fitnessLevel = 135;
            if (mileStones[26].position.x < transform.position.x
              && (mileStones[26].position.x + 1) > transform.position.x) {
                fitnessLevel += 71;
            }
            fitness += fitnessLevel;
            currentAtMilestone[26] = current;
        } else if (passedMileStones[0]
                && passedMileStones[1]
                && passedMileStones[2]
                && passedMileStones[3]
                && passedMileStones[4]
                && passedMileStones[5]
                && passedMileStones[6]
                && passedMileStones[7]
                && passedMileStones[8]
                && passedMileStones[9]
                && passedMileStones[10]
                && passedMileStones[11]
                && passedMileStones[12]
                && passedMileStones[13]
                && passedMileStones[14]
                && passedMileStones[15]
                && passedMileStones[16]
                && passedMileStones[17]
                && passedMileStones[18]
                && passedMileStones[19]
                && passedMileStones[20]
                && passedMileStones[21]
                && passedMileStones[22]
                && passedMileStones[23]
                && passedMileStones[24]
                && passedMileStones[25]
                && passedMileStones[26]
                && !passedMileStones[27]
                && mileStones[27].position.y < transform.position.y) {
            passedMileStones[27] = true;
            fitnessLevel = 140;
            if (mileStones[27].position.x < transform.position.x
              && (mileStones[27].position.x + 1) > transform.position.x) {
                fitnessLevel += 73;
            }
            fitness += fitnessLevel;
            currentAtMilestone[27] = current;
        } else if (passedMileStones[0]
                && passedMileStones[1]
                && passedMileStones[2]
                && passedMileStones[3]
                && passedMileStones[4]
                && passedMileStones[5]
                && passedMileStones[6]
                && passedMileStones[7]
                && passedMileStones[8]
                && passedMileStones[9]
                && passedMileStones[10]
                && passedMileStones[11]
                && passedMileStones[12]
                && passedMileStones[13]
                && passedMileStones[14]
                && passedMileStones[15]
                && passedMileStones[16]
                && passedMileStones[17]
                && passedMileStones[18]
                && passedMileStones[19]
                && passedMileStones[20]
                && passedMileStones[21]
                && passedMileStones[22]
                && passedMileStones[23]
                && passedMileStones[24]
                && passedMileStones[25]
                && passedMileStones[26]
                && passedMileStones[27]
                && !passedMileStones[28]
                && mileStones[28].position.x < transform.position.x) {
            passedMileStones[28] = true;
            fitnessLevel = 145;
            if (mileStones[28].position.y < transform.position.y
              && (mileStones[28].position.y + 1) > transform.position.y) {
                fitnessLevel += 74;
            }
            fitness += fitnessLevel;
            currentAtMilestone[28] = current;
        } else if (passedMileStones[0]
                && passedMileStones[1]
                && passedMileStones[2]
                && passedMileStones[3]
                && passedMileStones[4]
                && passedMileStones[5]
                && passedMileStones[6]
                && passedMileStones[7]
                && passedMileStones[8]
                && passedMileStones[9]
                && passedMileStones[10]
                && passedMileStones[11]
                && passedMileStones[12]
                && passedMileStones[13]
                && passedMileStones[14]
                && passedMileStones[15]
                && passedMileStones[16]
                && passedMileStones[17]
                && passedMileStones[18]
                && passedMileStones[19]
                && passedMileStones[20]
                && passedMileStones[21]
                && passedMileStones[22]
                && passedMileStones[23]
                && passedMileStones[24]
                && passedMileStones[25]
                && passedMileStones[26]
                && passedMileStones[27]
                && passedMileStones[28]
                && !passedMileStones[29]
                && mileStones[29].position.x < transform.position.x) {
            passedMileStones[29] = true;
            fitnessLevel = 150;
            if (mileStones[29].position.y < transform.position.y
              && (mileStones[29].position.y + 1) > transform.position.y) {
                fitnessLevel += 76;
            }
            fitness += fitnessLevel;
            currentAtMilestone[29] = current;
        } else if (passedMileStones[0]
                && passedMileStones[1]
                && passedMileStones[2]
                && passedMileStones[3]
                && passedMileStones[4]
                && passedMileStones[5]
                && passedMileStones[6]
                && passedMileStones[7]
                && passedMileStones[8]
                && passedMileStones[9]
                && passedMileStones[10]
                && passedMileStones[11]
                && passedMileStones[12]
                && passedMileStones[13]
                && passedMileStones[14]
                && passedMileStones[15]
                && passedMileStones[16]
                && passedMileStones[17]
                && passedMileStones[18]
                && passedMileStones[19]
                && passedMileStones[20]
                && passedMileStones[21]
                && passedMileStones[22]
                && passedMileStones[23]
                && passedMileStones[24]
                && passedMileStones[25]
                && passedMileStones[26]
                && passedMileStones[27]
                && passedMileStones[28]
                && passedMileStones[29]
                && !passedMileStones[30]
                && mileStones[30].position.x < transform.position.x) {
            passedMileStones[30] = true;
            fitnessLevel = 170;
            if (mileStones[30].position.y < transform.position.y
              && (mileStones[30].position.y + 1) > transform.position.y) {
                fitnessLevel += 82;
            }
            fitness += fitnessLevel;
            currentAtMilestone[30] = current;
        } else if (passedMileStones[0]
                && passedMileStones[1]
                && passedMileStones[2]
                && passedMileStones[3]
                && passedMileStones[4]
                && passedMileStones[5]
                && passedMileStones[6]
                && passedMileStones[7]
                && passedMileStones[8]
                && passedMileStones[9]
                && passedMileStones[10]
                && passedMileStones[11]
                && passedMileStones[12]
                && passedMileStones[13]
                && passedMileStones[14]
                && passedMileStones[15]
                && passedMileStones[16]
                && passedMileStones[17]
                && passedMileStones[18]
                && passedMileStones[19]
                && passedMileStones[20]
                && passedMileStones[21]
                && passedMileStones[22]
                && passedMileStones[23]
                && passedMileStones[24]
                && passedMileStones[25]
                && passedMileStones[26]
                && passedMileStones[27]
                && passedMileStones[28]
                && passedMileStones[29]
                && passedMileStones[30]
                && !passedMileStones[31]
                && mileStones[31].position.x < transform.position.x) {
            passedMileStones[31] = true;
            fitnessLevel = 185;
            if (mileStones[31].position.y < transform.position.y
              && (mileStones[31].position.y + 1) > transform.position.y) {
                fitnessLevel += 88;
            }
            fitness += fitnessLevel;
            currentAtMilestone[31] = current;
        } else if (passedMileStones[0]
                 && passedMileStones[1]
                 && passedMileStones[2]
                 && passedMileStones[3]
                 && passedMileStones[4]
                 && passedMileStones[5]
                 && passedMileStones[6]
                 && passedMileStones[7]
                 && passedMileStones[8]
                 && passedMileStones[9]
                 && passedMileStones[10]
                 && passedMileStones[11]
                 && passedMileStones[12]
                 && passedMileStones[13]
                 && passedMileStones[14]
                 && passedMileStones[15]
                 && passedMileStones[16]
                 && passedMileStones[17]
                 && passedMileStones[18]
                 && passedMileStones[19]
                 && passedMileStones[20]
                 && passedMileStones[21]
                 && passedMileStones[22]
                 && passedMileStones[23]
                 && passedMileStones[24]
                 && passedMileStones[25]
                 && passedMileStones[26]
                 && passedMileStones[27]
                 && passedMileStones[28]
                 && passedMileStones[29]
                 && passedMileStones[30]
                 && passedMileStones[31]
                 && !passedMileStones[32]
                 && mileStones[32].position.x < transform.position.x) {
            passedMileStones[32] = true;
            fitnessLevel = 200;
            if (mileStones[32].position.y < transform.position.y
              && (mileStones[32].position.y + 1) > transform.position.y) {
                fitnessLevel += 95;
            }
            fitness += fitnessLevel;
            currentAtMilestone[32] = current;
        } else if (passedMileStones[0]
                 && passedMileStones[1]
                 && passedMileStones[2]
                 && passedMileStones[3]
                 && passedMileStones[4]
                 && passedMileStones[5]
                 && passedMileStones[6]
                 && passedMileStones[7]
                 && passedMileStones[8]
                 && passedMileStones[9]
                 && passedMileStones[10]
                 && passedMileStones[11]
                 && passedMileStones[12]
                 && passedMileStones[13]
                 && passedMileStones[14]
                 && passedMileStones[15]
                 && passedMileStones[16]
                 && passedMileStones[17]
                 && passedMileStones[18]
                 && passedMileStones[19]
                 && passedMileStones[20]
                 && passedMileStones[21]
                 && passedMileStones[22]
                 && passedMileStones[23]
                 && passedMileStones[24]
                 && passedMileStones[25]
                 && passedMileStones[26]
                 && passedMileStones[27]
                 && passedMileStones[28]
                 && passedMileStones[29]
                 && passedMileStones[30]
                 && passedMileStones[31]
                 && passedMileStones[32]
                 && !passedMileStones[33]
                 && mileStones[33].position.x < transform.position.x) {
            passedMileStones[33] = true;
            fitnessLevel = 210;
            if (mileStones[33].position.y < transform.position.y
              && (mileStones[33].position.y + 1) > transform.position.y) {
                fitnessLevel += 97;
            }
            fitness += fitnessLevel;
            currentAtMilestone[33] = current;
        }
    }
    void calculateFitness() {
        //instead of taking the distance to the goal, take distance to milestones
        double dist = Math.Sqrt(Math.Pow((double)(transform.position.x - goalTransform.position.x), 2) +
            Math.Pow((double)(transform.position.y - goalTransform.position.y), 2));
        getFitnessLevel();
        double currentFitness = maxDist - dist;
        if (dist < 1) {
            currentFitness *= 1.5;
        }
        if (!crashed) {
            //fitness += fitnessLevel;
        }

    }
}

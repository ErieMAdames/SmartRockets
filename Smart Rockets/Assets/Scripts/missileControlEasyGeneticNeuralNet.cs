using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class missileControlEasyGeneticNeuralNet : MonoBehaviour {
    // Start is called before the first frame update
    [SerializeField]
    public float speed;
    public float slerpRate;
    private int count;
    private Vector2 vel;
    Rigidbody2D rb;
    public bool isReady;
    public float[,,] weights;
    public float[] lastWeights;
    public int current;
    public Transform goalTransform;
    public double fitness;
    public double fitnessTime;
    public bool finished;
    private bool crashed;
    public bool reachedGoal;
    private double maxDist;
    public Transform mileStone;
    public bool passedMileStone;
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
        Physics2D.gravity = Vector2.zero;
        exploded = false;
        passedMileStone = false;
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
            Physics2D.gravity = new Vector2(0, -9.8f);
            GetComponent<MeshRenderer>().enabled = false;
            if (!exploded) {
                exploded = true;
                ParticleSystem[] ps = GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem child in ps) {
                    child.Stop();
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
        for (int i = 0; i < 5; i++) {
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
        string p = "";
        for(int i = 0; i < 9; i++) {
            p += normalizedArray[i] + " ";
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
    void calculateFitness() {
        double dist = Math.Sqrt(Math.Pow((double)(transform.position.x - goalTransform.position.x), 2) +
            Math.Pow((double)(transform.position.y - goalTransform.position.y), 2));

        double currentFitness = maxDist - dist;
        if (dist < 1) {
            currentFitness *= 2;
        }
        if (mileStone.position.y < transform.position.y) {
            passedMileStone = true;
            currentFitness *= 2;
        }
        if (!crashed) {
            fitness = currentFitness;
        }

    }
}

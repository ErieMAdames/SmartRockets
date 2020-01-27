using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketController : MonoBehaviour {
    // Start is called before the first frame update
    Rigidbody2D rb;
    bool goForward;
    public GameObject explosion;
    private bool exploded;
    public Transform startPos;
    private bool flameEnabled;
    private bool flameBurning;

    void Start() {
        rb = GetComponent<Rigidbody2D>();
    }
    void OnCollisionEnter2D(Collision2D collision) {
        if (collision.gameObject.tag == "wall") {
            rb.velocity = Vector3.zero;
            rb.freezeRotation = true;
            GetComponentInChildren<MeshRenderer>().enabled = false;
            if (!exploded) {
                exploded = true;
                ParticleSystem[] ps = GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem child in ps) {
                    child.Stop();
                    child.Clear();
                }
                GameObject expl = Instantiate(explosion, transform.position, Quaternion.identity) as GameObject;
                Destroy(expl, 2);
            }
        }
        if (collision.gameObject.tag == "Goal") {
            rb.velocity = Vector3.zero;
            rb.freezeRotation = true;
            GetComponentInChildren<MeshRenderer>().enabled = false;
            if (!exploded) {
                exploded = true;
                ParticleSystem[] ps = GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem child in ps) {
                    child.Stop();
                    child.Clear();
                }
                GameObject expl = Instantiate(explosion, transform.position, Quaternion.identity) as GameObject;
                Destroy(expl, 2);
            }
        }
    }
    // Update is called once per frame
    void flames() {
        if (flameEnabled) {
            if (!flameBurning) {
                ParticleSystem[] ps = GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem child in ps) {
                    child.Play();
                }
                flameBurning = true;
            }
        } else {
            ParticleSystem[] ps = GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem child in ps) {
                child.Stop();
            }
            flameBurning = false;
        }
    } 
    void Update() {
        flames();
        if (exploded) {
            GetComponentInChildren<MeshRenderer>().enabled = true;
            exploded = false;
            transform.position = startPos.position;
            transform.rotation = Quaternion.identity;
        }
        if (Input.GetKeyDown(KeyCode.Space)) {
            goForward = true;
            if (goForward) {
                flameEnabled = true;
                flames();
                rb.velocity = transform.up * 10;
            } else {
                rb.velocity = Vector3.zero;
            }
        }
        if (Input.GetKeyUp(KeyCode.Space)) {
            rb.velocity = Vector3.zero;
            goForward = false;
            flameEnabled = false;
            flames();
        }
        if (Input.GetKey(KeyCode.LeftArrow)) {
            transform.Rotate(new Vector3(0, 0, 5));
            if (goForward) {
                rb.velocity = transform.up * 10;
            } else {
                rb.velocity = Vector3.zero;
            }
        }
        if (Input.GetKeyUp(KeyCode.LeftArrow)) {
            transform.Rotate(new Vector3(0, 0, 0));
            if (goForward) {
                rb.velocity = transform.up * 10;
            } else {
                rb.velocity = Vector3.zero;
            }
        }
        if (Input.GetKey(KeyCode.RightArrow)) {
            transform.Rotate(new Vector3(0, 0, -5));
            if (goForward) {
                rb.velocity = transform.up * 10;
            } else {
                rb.velocity = Vector3.zero;
            }
        }
        if (Input.GetKeyUp(KeyCode.RightArrow)) {
            transform.Rotate(new Vector3(0, 0, 0));
            if (goForward) {
                rb.velocity = transform.up * 10;
            } else {
                rb.velocity = Vector3.zero;
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class BezierFollow : MonoBehaviour {
    [SerializeField]
    public GameObject rocketPrefab;
    public Transform route;
    private Vector2 position;
    private float speed;
    private bool coroutineAllowed;
    private GameObject rocket;


    // Start is called before the first frame update
    void Start() {
        speed = .01f;
        coroutineAllowed = true;
        Quaternion rotation = new Quaternion(0, 0, 0, 1);
        rocket = Instantiate(rocketPrefab, transform.position, rotation) as GameObject;
    }

    // Update is called once per frame
    void Update() {
        if (coroutineAllowed) {
            StartCoroutine(travelRoute());
        }

    }
    private IEnumerator travelRoute() {
        coroutineAllowed = false;
        int numChildren = route.childCount;
        for (int i = 1; i < numChildren; i++) {
            float t = 0;
            Vector3 p0 = route.GetChild(i - 1).position;
            Vector3 p1 = route.GetChild(i).position;
            while (t < 1) {
                t += Time.deltaTime * speed;
                position = (1 - t) * p0 +
                    (t) * p1;
                if (Vector3.Distance(position, transform.position) > .5f) {
                    float x = position.x - transform.position.x;
                    float y = position.y - transform.position.y;
                    float angle = (float)(Math.Atan2(y, x) * (180 / Math.PI) - 90);
                    Quaternion target = Quaternion.Euler(0, 0, (float)angle);
                    rocket.transform.rotation = target;
                    rocket.transform.position = position;
                    transform.position = position;
                    yield return new WaitForEndOfFrame();
                }
            }
        }
        coroutineAllowed = true;
    }

}

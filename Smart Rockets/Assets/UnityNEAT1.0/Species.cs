using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Species {
    public int speciesIdentifier;
    public List<NetworkWrapper> organisms;
    public NetworkWrapper representative;

    public Species(int speciesIdentifier) {
        this.speciesIdentifier = speciesIdentifier;
        organisms = new List<NetworkWrapper>();
    }
    public void add(NetworkWrapper netWrapper) {
        organisms.Add(netWrapper);
    }
    public void setRepresentative() {
        representative = organisms[organisms.Count / 2];
    }
}
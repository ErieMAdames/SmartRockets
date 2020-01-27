using System.Collections;
using System.Collections.Generic;
using System;

public class Node {
    public int id;
    public int layer;
    public Dictionary<int, double> weights;
    public Dictionary<int, bool> connectionEnabledDisabled;
    public bool isInput;
    public NeuralNet parent;

    public Node(int id, NeuralNet parent) {
        layer = 0;
        isInput = true;
        this.id = id;
        this.parent = parent;
        connectionEnabledDisabled = new Dictionary<int, bool>();
        parent.layersOfNodes[id] = layer;
    }
    public void setLayer(int layer) {
        this.layer = layer;
        parent.layersOfNodes[id] = layer;
    }
    public Node(int id, Dictionary<int, double> weights, NeuralNet parent) {
        isInput = false;
        this.id = id;
        this.weights = weights;
        this.parent = parent;
        connectionEnabledDisabled = new Dictionary<int, bool>();
        foreach (int node in weights.Keys) {
            connectionEnabledDisabled.Add(node, true);
        }
    }
    public void augmentNodeWeights(int nodeFrom, double weight) {
        weights.Add(nodeFrom, weight);
        connectionEnabledDisabled.Add(nodeFrom, true);
    }
    public void disableConnection(int nodeFromId) {
        connectionEnabledDisabled[nodeFromId] = false;
    }
    public double getOutput() {
        double acc = 0;
        if (layer == 0) {
            return parent.nodeValues[id];
        } else {
            foreach (int key in connectionEnabledDisabled.Keys) {
                if (connectionEnabledDisabled[key]) {
                    acc += parent.nodeValues[key] * weights[key];
                }
            }
        }
        return Math.Tanh(acc);
    }
}

using System.Collections;
using System.Collections.Generic;

public class NetworkWrapper {
    public NeuralNet network;
    public double fitness;
    public NetworkWrapper(NeuralNet neuralNet) {
        network = neuralNet;
    }
}
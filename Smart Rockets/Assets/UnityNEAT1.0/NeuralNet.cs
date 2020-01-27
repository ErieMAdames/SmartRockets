using System.Collections;
using System.Collections.Generic;
using System;

public class NeuralNet {
    public Dictionary<int, NetworkLayer> layers;
    public Dictionary<int, int> layersOfNodes;
    public Dictionary<int, double> nodeValues;
    public HashSet<int> nodes;
    public int[] outputNodes;
    public int numInputs, numOutputs;
    int depth, nodeIdCounter;
    public int numLayers;
    public Dictionary<int, int[]> innovations;

    public NeuralNet(int numInputs, int numOutputs, Random random) {
        nodeIdCounter = numInputs;
        this.numInputs = numInputs;
        this.numOutputs = numOutputs;
        depth = 1;
        layers = new Dictionary<int, NetworkLayer>();
        layersOfNodes = new Dictionary<int, int>();
        nodeValues = new Dictionary<int, double>();
        createInputLayer();
        Node[] outNodes = new Node[numOutputs];
        innovations = new Dictionary<int, int[]>();
        nodes = new HashSet<int>();
        for (int i = 0; i < numOutputs; i++) {
            Dictionary<int, double> weights = new Dictionary<int, double>();
            for (int j = 0; j < numInputs; j++) {
                weights.Add(j, random.NextDouble() * (2.0) - 1.0);
            }
            outNodes[i] = new Node(nodeIdCounter, weights, this);
            nodeIdCounter++;
        }
        createOutputLayer(outNodes);
        numLayers = 1;
    }
    public NeuralNet(NeuralNet neural) {
        nodeIdCounter = neural.nodeIdCounter;
        numInputs = neural.numInputs;
        numOutputs = neural.numOutputs;
        depth = neural.depth;
        layers = new Dictionary<int, NetworkLayer>();
        layersOfNodes = new Dictionary<int, int>();
        nodeValues = new Dictionary<int, double>();
        createInputLayer();
        nodes = new HashSet<int>();
        numLayers = neural.numLayers;
        innovations = new Dictionary<int, int[]>();
        foreach (int inn in neural.innovations.Keys) {
            innovate(inn, neural.innovations[inn][0], neural.innovations[inn][1]);
        }
        outputNodes = new int[numOutputs];
        foreach (NetworkLayer layer in neural.layers.Values) {
            if (layer.layerType > 0) {
                NetworkLayer newLayer = new NetworkLayer(layer.layerNumber, layer.layerType, this);
                int i = 0;
                foreach (Node node in layer.nodes.Values) {
                    Dictionary<int, double> weights = new Dictionary<int, double>(node.weights);
                    Dictionary<int, bool> connectionEnabledDisabled = new Dictionary<int, bool>(node.connectionEnabledDisabled);
                    Node newNode = new Node(node.id, weights, this);
                    newNode.connectionEnabledDisabled = connectionEnabledDisabled;
                    newNode.setLayer(newLayer.layerNumber);
                    newLayer.addNode(newNode);
                    nodes.Add(newNode.id);
                    if (layer.layerType == 2) {
                        outputNodes[i] = newNode.id;
                        i++;
                    }
                }
                layers.Add(newLayer.layerNumber, newLayer);
            }
        }
    }
    public void shiftUpLayers(int startingLayer) {
        for (int i = depth; i >= startingLayer; i--) {
            if (layers.ContainsKey(i)) {
                layers[i].layerNumber += 1;
                layers.Add(i + 1, layers[i]);
                foreach (Node node in layers[i].nodes.Values) {
                    node.setLayer(i + 1);
                }
                layers.Remove(i);
            }
        }
        depth++;
    }
    public int getAndUpdateNodeIdCounter() {
        nodeIdCounter++;
        return nodeIdCounter - 1;
    }
    public void innovate(int innovationNumber, int from, int to) {
        innovations.Add(innovationNumber, new int[] { from, to });
    }
    private void createInputLayer() {
        NetworkLayer inputLayer = new NetworkLayer(0, 0, this);
        inputLayer.createInputLayer();
        layers.Add(0, inputLayer);
    }
    public void createLayer(Node[] nodesToAdd, int layerId) {
        NetworkLayer layer = new NetworkLayer(layerId, 1, this);
        foreach (Node node in nodesToAdd) {
            layer.addNode(node);
            node.setLayer(layerId);
            nodes.Add(node.id);
        }
        layers.Add(layerId, layer);
        numLayers++;
    }
    public void createOutputLayer(Node[] nodesToAdd) {
        NetworkLayer layer = new NetworkLayer(1, 2, this);
        foreach (Node node in nodesToAdd) {
            layer.addNode(node);
            node.setLayer(1);
            nodes.Add(node.id);
        }
        layers.Add(1, layer);
        outputNodes = new int[nodesToAdd.Length];
        for (int i = 0; i < outputNodes.Length; i++) {
            outputNodes[i] = nodesToAdd[i].id;
        }
    }
    public Dictionary<int, double> getOutPuts(double[] inputs) {
        for (int i = 0; i < inputs.Length; i++) {
            nodeValues[i] = inputs[i];
        }
        Dictionary<int, double> outputs = new Dictionary<int, double>();
        for (int i = 0; i < depth; i++) {
            layers[i].layerOutput();
        }
        layers[depth].layerOutput();
        Dictionary<int, double> finalOutputs = new Dictionary<int, double>();
        Array.Sort(outputNodes);
        for (int i = 0; i < outputNodes.Length; i++) {
            finalOutputs.Add(i, nodeValues[outputNodes[i]]);
        }
        return finalOutputs;
    }
    public class NetworkLayer {
        public int layerType;
        public int layerNumber;
        public Dictionary<int, Node> nodes;
        public int numNodes;
        public NeuralNet parent;

        public NetworkLayer(int layerNumber, int layerType, NeuralNet parent) {
            this.layerNumber = layerNumber;
            this.layerType = layerType;
            this.parent = parent;
            nodes = new Dictionary<int, Node>();
            numNodes = 0;
        }
        public void createInputLayer() {
            if (layerType == 0) {
                for (int i = 0; i < parent.numInputs; i++) {
                    nodes.Add(i, new Node(i, parent));
                    numNodes++;
                }
            }
        }
        public void addNode(Node node) {
            if (layerType > 0) {
                node.setLayer(layerNumber);
                nodes.Add(node.id, node);
                numNodes++;
            }
        }
        public void layerOutput() {
            if (layerType != 0) {
                foreach (int n in nodes.Keys) {
                    parent.nodeValues[n] = nodes[n].getOutput();
                }
            }
        }
    }
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
}
using System.Collections;
using System.Collections.Generic;

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
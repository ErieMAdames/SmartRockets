using System.Collections;
using System.Collections.Generic;
using System;

public class EvolutionController {
    int numInputs, numOutputs, populationSize, maxSpecies;
    double mutationRate;
    public Dictionary<int, int[]> innovations;
    public Dictionary<string, int> reverseInnovations;
    int innovationCounter;
    public NetworkWrapper[] networks;
    private Random random;
    private int speciesCounter;
    public List<Species> species;
    double speciationDistance;

    public EvolutionController(int numInputs, int numOutputs, int populationSize, double mutationRate, double speciationDistance = .5) {
        this.mutationRate = mutationRate;
        this.numInputs = numInputs;
        this.numOutputs = numOutputs;
        this.populationSize = populationSize;
        random = new Random();
        innovations = new Dictionary<int, int[]>();
        reverseInnovations = new Dictionary<string, int>();
        innovationCounter = 0;
        speciesCounter = 0;
        species = new List<Species>();
        this.speciationDistance = speciationDistance;
    }
    public void initNetworks() {
        networks = new NetworkWrapper[populationSize];
        for (int i = 0; i < numInputs; i++) {
            for (int j = numInputs; j < numOutputs + numInputs; j++) {
                innovations.Add(innovationCounter, new int[] { i, j });
                innovationCounter++;
            }
        }
        int temp = innovationCounter;
        innovationCounter = 0;
        for (int i = 0; i < populationSize; i++) {
            networks[i] = new NetworkWrapper(new NeuralNet(numInputs, numOutputs, random));
            for (int j = 0; j < numInputs; j++) {
                for (int k = numInputs; k < numOutputs + numInputs; k++) {
                    networks[i].network.innovate(innovationCounter, j, k);
                    innovationCounter++;
                }
            }
            innovationCounter = 0;
        }
        innovationCounter = temp;
        speciate();
    }
    public double computeCompatabilityDistance(NetworkWrapper n1, NetworkWrapper n2) {
        List<int> n1List = new List<int>(n1.network.innovations.Keys);
        List<int> n2List = new List<int>(n2.network.innovations.Keys);
        n1List.Sort();
        n2List.Sort();
        int[] p1 = n1List.ToArray();
        int[] p2 = n2List.ToArray();
        double excess = 0;
        if (p1[p1.Length - 1] != p2[p2.Length - 1]) {
            if (p1[p1.Length - 1] > p2[p2.Length - 1]) {
                for (int i = p1.Length - 1; i >= 0; i--) {
                    if (n2List.Contains(p1[i])) {
                        break;
                    } else {
                        excess++;
                    }
                }
            } else {
                for (int i = p2.Length - 1; i >= 0; i--) {
                    if (n1List.Contains(p2[i])) {
                        break;
                    } else {
                        excess++;
                    }
                }
            }
        }
        double disjoint = 0 - excess;
        double totalWeightDiff = 0;
        int totalForAverage = 0;
        for (int i = 0; i < p1.Length; i++) {
            if (!n2List.Contains(p1[i])) {
                disjoint++;
            } else {
                int nodeTo = innovations[p1[i]][1];
                int nodeFrom = innovations[p1[i]][0];
                int layer = n1.network.layersOfNodes[nodeTo];
                double weight1 = n1.network.layers[layer].nodes[nodeTo].weights[nodeFrom];
                layer = n2.network.layersOfNodes[nodeTo];
                double weight2 = n2.network.layers[layer].nodes[nodeTo].weights[nodeFrom];
                totalWeightDiff += Math.Abs(weight2 - weight1);
                totalForAverage++;
            }
        }
        for (int i = 0; i < p2.Length; i++) {
            if (!n1List.Contains(p2[i])) {
                disjoint++;
            }
        }
        double averageWeightDiff = totalWeightDiff / totalForAverage;
        int N = Math.Max(p1.Length, p2.Length);
        double compatabilityDistance = (disjoint / N) + (excess / N) + averageWeightDiff;
        return compatabilityDistance;
    }
    public void speciate() {
        for (int i = 0; i < networks.Length; i++) {
            if (species.Count == 0) {
                Species newSpecies = new Species(speciesCounter);
                speciesCounter++;
                newSpecies.add(networks[i]);
                newSpecies.setRepresentative();
                species.Add(newSpecies);
            } else {
                bool speciesNotSet = true;
                foreach (Species specie in species) {
                    if (computeCompatabilityDistance(networks[i], specie.representative) <= speciationDistance) {
                        specie.add(networks[i]);
                        speciesNotSet = false;
                        break;
                    }
                }
                if (speciesNotSet) {
                    Species newSpecies = new Species(speciesCounter);
                    speciesCounter++;
                    newSpecies.add(networks[i]);
                    newSpecies.setRepresentative();
                    species.Add(newSpecies);
                }
            }
        }
        foreach (Species sp in species) {
            if (sp.organisms.Count > 0) {
                sp.setRepresentative();
            }
        }
    }
    public NetworkWrapper mate(NetworkWrapper parent1, NetworkWrapper parent2) {
        double parent1Chance = parent1.getFitness() / (parent1.getFitness() + parent2.getFitness());
        if (parent1.getFitness() > parent2.getFitness()) {
            NeuralNet child = new NeuralNet(parent1.network);
            foreach (int innovation in parent1.network.innovations.Keys) {
                if (parent2.network.innovations.ContainsKey(innovation)) {
                    if (random.NextDouble() > parent1Chance) {
                        int nodeTo = parent1.network.innovations[innovation][1];
                        int nodeFrom = parent1.network.innovations[innovation][0];
                        NeuralNet.NetworkLayer childLayer = child.layers[child.layersOfNodes[nodeTo]];
                        childLayer.nodes[nodeTo].weights[nodeFrom] = parent2.network.layers[parent2.network.layersOfNodes[nodeTo]].nodes[nodeTo].weights[nodeFrom];
                    }
                }
            }
            NetworkWrapper childWrapper = new NetworkWrapper(child);
            mutate(childWrapper);
            return childWrapper;
        } else {
            NeuralNet child = new NeuralNet(parent2.network);
            foreach (int innovation in parent2.network.innovations.Keys) {
                if (parent1.network.innovations.ContainsKey(innovation)) {
                    if (random.NextDouble() < parent1Chance) {
                        int nodeTo = parent1.network.innovations[innovation][1];
                        int nodeFrom = parent1.network.innovations[innovation][0];
                        NeuralNet.NetworkLayer childLayer = child.layers[child.layersOfNodes[nodeTo]];
                        childLayer.nodes[nodeTo].weights[nodeFrom] = parent1.network.layers[parent1.network.layersOfNodes[nodeTo]].nodes[nodeTo].weights[nodeFrom];
                    }
                }
            }
            NetworkWrapper childWrapper = new NetworkWrapper(child);
            mutate(childWrapper);
            return childWrapper;
        }
    }
    public void mutate(NetworkWrapper netWrapper) {
        if (random.NextDouble() < mutationRate) {
            double p = random.NextDouble();
            if (p < .25) {
                mutateAddNode(netWrapper);
            } else if (p >= .25 && p < .5) {
                mutateAddConnection(netWrapper);
            } else if (p >= .5 && p < .75) {
                mutateRandomizeWeight(netWrapper);
            } else {
                mutateShiftWeight(netWrapper);
            }
        }
    }
    public void mutateRandomizeWeight(NetworkWrapper netWrapper) {
        int rand = random.Next(netWrapper.network.nodes.Count);
        int ii = 0;
        foreach (int nodeId in netWrapper.network.nodes) {
            if (ii == rand) {
                NeuralNet.Node node = netWrapper.network.layers[netWrapper.network.layersOfNodes[nodeId]].nodes[nodeId];
                rand = random.Next(node.weights.Count);
                ii = 0;
                foreach (int n in node.weights.Keys) {
                    if (rand == ii) {
                        node.weights[n] = random.NextDouble() * (2.0) - 1.0;
                        break;
                    }
                    ii++;
                }
                break;
            }
            ii++;
        }
    }
    public void mutateShiftWeight(NetworkWrapper netWrapper) {
        int rand = random.Next(netWrapper.network.nodes.Count);
        int ii = 0;
        foreach (int nodeId in netWrapper.network.nodes) {
            if (ii == rand) {
                //select random node from prev layers
                NeuralNet.Node node = netWrapper.network.layers[netWrapper.network.layersOfNodes[nodeId]].nodes[nodeId];
                rand = random.Next(node.weights.Count);
                ii = 0;
                foreach (int n in node.weights.Keys) {
                    if (rand == ii) {
                        node.weights[n] += random.NextDouble() * (1.0) - .5;
                        break;
                    }
                    ii++;
                }
                break;
            }
            ii++;
        }
    }
    public void mutateAddConnection(NetworkWrapper netWrapper) {
        int rand = random.Next(netWrapper.network.nodes.Count);
        int ii = 0;
        foreach (int nodeId in netWrapper.network.nodes) {
            if (ii == rand) {
                //select random node from prev layers
                NeuralNet.Node next = netWrapper.network.layers[netWrapper.network.layersOfNodes[nodeId]].nodes[nodeId];
                rand = random.Next(0, next.layer - 1);
                NeuralNet.NetworkLayer layer = netWrapper.network.layers[rand];
                rand = random.Next(layer.numNodes);
                ii = 0;
                foreach (int nodeFrom in layer.nodes.Keys) {
                    if (rand == ii) {
                        if (!next.weights.ContainsKey(nodeFrom)) {
                            next.augmentNodeWeights(nodeFrom, random.NextDouble() * (2.0) - 1.0);
                            if (reverseInnovations.ContainsKey(nodeFrom + ":" + next.id)) {
                                netWrapper.network.innovate(reverseInnovations[nodeFrom + ":" + next.id], nodeFrom, next.id);
                            } else {
                                innovations.Add(innovationCounter, new int[] { nodeFrom, next.id });
                                reverseInnovations.Add(nodeFrom + ":" + next.id, innovationCounter);
                                netWrapper.network.innovate(innovationCounter, nodeFrom, next.id);
                                innovationCounter++;
                            }
                            break;
                        }
                    }
                    ii++;
                }
                break;
            }
            ii++;
        }
    }
    public void mutateAddNode(NetworkWrapper netWrapper) {
        int rand = random.Next(netWrapper.network.nodes.Count);
        int ii = 0;
        foreach (int nodeId in netWrapper.network.nodes) {
            if (ii == rand) {
                NeuralNet.Node next = netWrapper.network.layers[netWrapper.network.layersOfNodes[nodeId]].nodes[nodeId];
                int prevNodeId = -1;
                bool go = true;
                while (go) {
                    ii = 0;
                    rand = random.Next(next.weights.Count);
                    foreach (int node in next.weights.Keys) {
                        if (ii == rand) {
                            prevNodeId = node;
                            break;
                        }
                        ii++;
                    }
                    if (next.connectionEnabledDisabled[prevNodeId]) {
                        go = false;
                    }
                }
                NeuralNet.Node prev = netWrapper.network.layers[netWrapper.network.layersOfNodes[prevNodeId]].nodes[prevNodeId];
                Dictionary<int, double> weights = new Dictionary<int, double>() { { prev.id, 1 } };
                int midId = netWrapper.network.getAndUpdateNodeIdCounter();
                NeuralNet.Node mid = new NeuralNet.Node(midId, weights, netWrapper.network);
                next.disableConnection(prev.id);
                next.augmentNodeWeights(mid.id, next.weights[prev.id]);
                netWrapper.network.shiftUpLayers(netWrapper.network.layersOfNodes[nodeId]);
                netWrapper.network.createLayer(new NeuralNet.Node[] { mid }, netWrapper.network.layersOfNodes[nodeId] - 1);
                if (reverseInnovations.ContainsKey(prev.id + ":" + mid.id)) {
                    netWrapper.network.innovate(reverseInnovations[prev.id + ":" + mid.id], prev.id, mid.id);
                } else {
                    innovations.Add(innovationCounter, new int[] { prev.id, mid.id });
                    reverseInnovations.Add(prev.id + ":" + mid.id, innovationCounter);
                    netWrapper.network.innovate(innovationCounter, prev.id, mid.id);
                    innovationCounter++;
                }
                if (reverseInnovations.ContainsKey(mid.id + ":" + next.id)) {
                    netWrapper.network.innovate(reverseInnovations[mid.id + ":" + next.id], mid.id, next.id);
                } else {
                    innovations.Add(innovationCounter, new int[] { mid.id, next.id });
                    reverseInnovations.Add(mid.id + ":" + next.id, innovationCounter);
                    netWrapper.network.innovate(innovationCounter, mid.id, next.id);
                    innovationCounter++;
                }
                break;
            }
            ii++;
        }
    }
    public void stepForward() {
        adjustFitness();
        assignNumChildren();
        killOffLowPerformers();
        normalizeFitness();
        NetworkWrapper[] newNetworks = new NetworkWrapper[populationSize];
        int c = 0;
        foreach (Species specie in species) {
            List<NetworkWrapper> matingPool = new List<NetworkWrapper>();
            foreach (NetworkWrapper organism in specie.organisms) {
                for (int i = 0; i < organism.getNormalizedFitness(); i++) {
                    matingPool.Add(organism);
                }
            }
            for (int i = 0; i < specie.numChildren && c < populationSize; i++) {
                NetworkWrapper p1 = matingPool[random.Next(matingPool.Count - 1)];
                NetworkWrapper p2 = matingPool[random.Next(matingPool.Count - 1)];
                NetworkWrapper child = mate(p1, p2);
                mutate(child);
                newNetworks[c] = child;
                c++;
            }
            specie.organisms.Clear();
        }
        networks = newNetworks;
        speciate();
    }
    public void adjustFitness() {
        foreach (Species specie in species) {
            foreach (NetworkWrapper organism in specie.organisms) {
                organism.setAdjustedFitness(organism.getFitness() / specie.organisms.Count);
            }
        }
    }
    public void normalizeFitness() {
        foreach (Species specie in species) {
            double totalFitness = 0;
            foreach (NetworkWrapper networkWrapper in specie.organisms) {
                totalFitness += networkWrapper.getFitness();
            }
            foreach (NetworkWrapper networkWrapper in specie.organisms) {
                networkWrapper.setNormalizedFitness(Math.Floor(populationSize * networkWrapper.getFitness() / (totalFitness * 2)));
            }
        }
    }
    public void assignNumChildren() {
        double totalAdjustedFitness = 0;
        for (int i = 0; i < networks.Length; i++) {
            totalAdjustedFitness += networks[i].getAdjustedFitness();
        }
        int totalChildren = 0;
        foreach (Species specie in species) {
            double totalSpecieAdjustedFitness = 0;
            foreach (NetworkWrapper organism in specie.organisms) {
                totalSpecieAdjustedFitness += organism.getAdjustedFitness();
            }
            specie.numChildren = (int)Math.Round(populationSize * totalSpecieAdjustedFitness / totalAdjustedFitness, MidpointRounding.AwayFromZero);
            totalChildren += specie.numChildren;
        }
        foreach (Species specie in species) {
            if (totalChildren < populationSize) {
                if (random.NextDouble() < .2) {
                    specie.numChildren++;
                    totalChildren++;
                }
            } else {
                break;
            }
        }
    }
    public void killOffLowPerformers() {
        foreach (Species specie in species) {
            specie.organisms.Sort(0, specie.organisms.Count, new sortNetWorWrapper());
            int numToKill = specie.organisms.Count / 2;
            for (int i = 0; i < numToKill; i++) {
                specie.organisms.RemoveAt(0);
            }
            specie.setRepresentative();
        }
    }
    private double sharingFunction(double dist) {
        if (dist > speciationDistance) {
            return 0;
        } else {
            return 1;
        }
    }
    private class sortNetWorWrapper : IComparer<NetworkWrapper> {
        int IComparer<NetworkWrapper>.Compare(NetworkWrapper x, NetworkWrapper y) {
            if (x.getFitness() > y.getFitness()) {
                return 1;
            } else if (x.getFitness() > y.getFitness()) {
                return -1;
            } else {
                return 0;
            }
        }
    }
    public class NetworkWrapper {
        public NeuralNet network;
        private double fitness;
        private double adjustedFitness;
        private double normalizedFitness;
        public NetworkWrapper(NeuralNet neuralNet) {
            network = neuralNet;
        }
        public void setFitness(double fitness) {
            this.fitness = fitness;
        }
        public double getFitness() {
            return fitness;
        }
        public void setAdjustedFitness(double adjustedFitness) {
            this.adjustedFitness = adjustedFitness;
        }
        public double getAdjustedFitness() {
            return adjustedFitness;
        }
        public void setNormalizedFitness(double normalizedFitness) {
            this.normalizedFitness = normalizedFitness;
        }
        public double getNormalizedFitness() {
            return normalizedFitness;
        }
    }
    public class Species {
        public int speciesIdentifier;
        public List<NetworkWrapper> organisms;
        public NetworkWrapper representative;
        public int numChildren;

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
    //public void comp(NetworkWrapper c) {
    //    Console.WriteLine("--------1--------");
    //    foreach (NeuralNet.NetworkLayer layer in networks[0].network.layers.Values) {
    //        if (layer.layerType != 0)
    //            foreach (NeuralNet.Node node in layer.nodes.Values) {
    //                Console.WriteLine("Node : " + node.id);
    //                foreach (int i in node.weights.Keys) {
    //                    Console.WriteLine("n" + i + " - w" + node.weights[i]);
    //                }
    //            }
    //    }
    //    Console.WriteLine("--------2--------");
    //    foreach (NeuralNet.NetworkLayer layer in networks[1].network.layers.Values) {
    //        if (layer.layerType != 0)
    //            foreach (NeuralNet.Node node in layer.nodes.Values) {
    //                Console.WriteLine("Node : " + node.id);
    //                foreach (int i in node.weights.Keys) {
    //                    Console.WriteLine("n" + i + " - w" + node.weights[i]);
    //                }
    //            }
    //    }
    //    Console.WriteLine("--------3--------");
    //    foreach (NeuralNet.NetworkLayer layer in c.network.layers.Values) {
    //        if (layer.layerType != 0)
    //            foreach (NeuralNet.Node node in layer.nodes.Values) {
    //                Console.WriteLine("Node : " + node.id);
    //                foreach (int i in node.weights.Keys) {
    //                    Console.WriteLine("n" + i + " - w" + node.weights[i]);
    //                }
    //            }
    //    }
    //}
}
EvolutionController ec = new EvolutionController(3, 1, 100, .2, .53);
ec.initNetworks();
for (int i = 0; i< 100; i++) { if (i % 2 == 0) { ec.mutateAddConnection(ec.networks[i]); } else { ec.mutateAddNode(ec.networks[i]); } }
for (int i = 0; i< 100; i++) { if (i % 2 == 0) { ec.mutateAddConnection(ec.networks[i]); } else { ec.mutateAddNode(ec.networks[i]); } }
EvolutionController.NetworkWrapper n = ec.mate(ec.networks[0], ec.networks[1]);
for (int i = 0; i< 100; i++) { ec.networks[i].setFitness(i); }
ec.stepForward();
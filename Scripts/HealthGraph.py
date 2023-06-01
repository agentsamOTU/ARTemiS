import csv
import os
from matplotlib import pyplot as plt

print(os.getcwd())

times = [0.0]
healths=[100.0]

with open("Logs/agent-0.csv") as csvfile:
    csvreader = csv.reader(csvfile)
    for row in csvreader:
        if(row[0]=="INTERACTION"):
            times.append(float(row[1]))
            healths.append(float(row[6]))


plt.plot(times,healths)
plt.xlabel('Time')
plt.ylabel('Agent Health')
plt.savefig("Logs/graphs/agent-0.png")
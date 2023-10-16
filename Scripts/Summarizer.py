import csv
import os
from matplotlib import pyplot as plt

print(os.getcwd())

accuracy=0
evasion=0


combats=0
combatMisses=0
ies=0
ieTimes=0
penaltyTimeTotal=0
lastTime=0

with open("Logs/agent-0.csv") as csvfile:
    csvreader = csv.reader(csvfile)
    for row in csvreader:
        
        if(row[0]=="INTERACTIONEVENT"):
            print(row)
            ies+=1
            ieTimes=float(row[7])
            penaltyTimeTotal=float(row[9])-float(row[1])
            
        elif(row[0]=="COMBAT"):
            print(row)
            combats+=1
            combatMisses=int(row[3])
            penaltyTimeTotal=float(row[8])-float(row[1])
            
        elif(row[0]=="POSITION"):
            lastTime=float(row[1])
            
        elif(row[0]=="HEADER"):
            if(row[1]=="EVENTVALUES"):
                accuracy=float(row[3])
                evasion=float(row[5])
            
        
            
with open('Logs/summaries/agent-0-summary.csv', 'w', newline='') as csvfile:
     writer = csv.writer(csvfile)
     
     writer.writerow(["Combats",combats])
     writer.writerow(["CombatMisses",combatMisses])
     writer.writerow(["InteractionEvents",ies])
     writer.writerow(["IEPenalty",ieTimes])
     writer.writerow(["TimeLoss",penaltyTimeTotal])
     writer.writerow(["AgentTime",lastTime+penaltyTimeTotal])
     
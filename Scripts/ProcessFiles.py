import csv
import os
import shutil
from matplotlib import pyplot as plt

logDir="C:\\UnityProjects\\ARTemiS\\Logs"

if(not os.path.exists(logDir+os.sep+".logs"+os.sep+".pass0")):
    os.mkdir(logDir+os.sep+".logs"+os.sep+".pass0")
if(not os.path.exists(logDir+os.sep+".logs"+os.sep+".pass1")):
    os.mkdir(logDir+os.sep+".logs"+os.sep+".pass1")
if(not os.path.exists(logDir+os.sep+".logs"+os.sep+".pass2")):
    os.mkdir(logDir+os.sep+".logs"+os.sep+".pass2")
if(not os.path.exists(logDir+os.sep+"graphs")):
    os.mkdir(logDir+os.sep+"graphs")
if(not os.path.exists(logDir+os.sep+"summaries")):
    os.mkdir(logDir+os.sep+"summaries")       

    
for root, dirs, files in os.walk(logDir+os.sep+".logs"+os.sep+".pass0"):
    for dir in dirs:
        for file in os.listdir(root+os.sep+dir):
            with open(root+os.sep+dir+os.sep+file) as csvfile:
                csvreader = csv.reader(csvfile)
                times = [0.0]
                healths=[100.0]
                for row in csvreader:
                    if(row[0]=="INTERACTION"):
                        times.append(float(row[1]))
                        healths.append(float(row[6]))
                plt.plot(times,healths)
                plt.xlabel('Time')
                plt.ylabel('Agent Health')
                fname = logDir+os.sep+"graphs"+os.sep+dir+file
                plt.savefig(fname.replace(".csv",".jpg"))
                plt.cla()
        shutil.move(root+os.sep+dir,logDir+os.sep+".logs"+os.sep+".pass1"+os.sep+dir)
        
for root, dirs, files in os.walk(logDir+os.sep+".logs"+os.sep+".pass1"):
    for dir in dirs:
        for file in os.listdir(root+os.sep+dir):
            with open(root+os.sep+dir+os.sep+file) as csvfile:
                accuracy=0
                evasion=0
                combats=0
                combatMisses=0
                ies=0
                ieLow=0
                ieMed=0
                ieHigh=0
                ieTimes=0
                penaltyTimeTotal=0
                lastTime=0
                playerDied=False
                finalGoal=False
                with open(root+os.sep+dir+os.sep+file) as csvfile:
                    csvreader = csv.reader(csvfile)
                    for row in csvreader:

                        if(row[0]=="INTERACTIONEVENT"):
                            ies+=1
                            ieLow=float(row[4])
                            ieMed=float(row[5])
                            ieHigh=float(row[6])
                            ieTimes=float(row[7])
                            penaltyTimeTotal=float(row[9])-float(row[1])

                        elif(row[0]=="COMBAT"):
                            combats+=1
                            combatMisses=int(row[3])
                            penaltyTimeTotal=float(row[8])-float(row[1])

                        elif(row[0]=="POSITION"):
                            lastTime=float(row[1])

                        elif(row[0]=="HEADER"):
                            if(row[1]=="EVENTVALUES"):
                                accuracy=float(row[3])
                                evasion=float(row[5])
                        
                        elif(row[0]=="INTERACTION"):
                            if(float(row[6])<=0):
                                playerDied=True
                            if(row[7]=="ET_GOAL_COMPLETION"):
                                finalGoal=True
                                
                with open(logDir+os.sep+"summaries"+os.sep+dir+"summary"+file, 'w', newline='') as csvfile:
                     writer = csv.writer(csvfile)

                     writer.writerow(["Accuracy",accuracy])
                     writer.writerow(["Evasion",evasion])
                     writer.writerow(["Combats",combats])
                     writer.writerow(["CombatMisses",combatMisses])
                     writer.writerow(["True Accuracy",combats/(combatMisses+combats)])
                     writer.writerow(["InteractionEvents",ies])
                     writer.writerow(["IE Low Time",ieLow])
                     writer.writerow(["IE Medium Time",ieMed])
                     writer.writerow(["IE High Time",ieHigh])
                     writer.writerow(["IEPenalty",ieTimes])
                     writer.writerow(["TimeLoss",penaltyTimeTotal])
                     writer.writerow(["AgentTime",lastTime+penaltyTimeTotal])
                     writer.writerow(["PlayerDied",playerDied])
                     writer.writerow(["FinalGoalAchieved",finalGoal])


                     
                               
        shutil.move(root+os.sep+dir,logDir+os.sep+".logs"+os.sep+".pass2"+os.sep+dir)

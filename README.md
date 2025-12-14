# TubeTrekker

TubeTrekker is a specialized journey planning application designed for the London Underground. Unlike standard mapping software, this project focuses on providing optimized routes and accurate pricing specifically for the Tube network, leveraging live Transport for London (TfL) data. I implemented it as my technical solution for AQA A-Level Computer Science.

## Project Overview

The goal of TubeTrekker is to solve the complexities of navigating the London Underground. While traditional maps can be confusing and many existing digital solutions prioritize walking or bus routes, TubeTrekker is built to handle the unique constraints of the Underground, such as line transfers, peak/off-peak pricing, and live service disruptions.

## Technical Approach: A* Pathfinding

For route calculation, I implemented the **A* Search Algorithm** rather than a standard Dijkstra approach.

* **Efficiency**: A* utilizes a heuristic (in this case, diagonal distance between station coordinates) to guide the search toward the destination.
* **Optimization**: By using a heuristic, the algorithm significantly reduces the number of nodes explored compared to Dijkstra, resulting in much faster response times for the user.
* **Graph Structure**: The network is represented as a weighted graph using adjacency lists, where weights are determined by the average travel time between stations.

## Engineering Practices & Growth

This project served as a major learning milestone in my software engineering journey.

* **Model-View-Controller (MVC) Reflection**: While TubeTrekker is fully functional, looking back, the architecture lacks strict separation between the logic and the UI. If I were to build this again, I would implement a robust MVC or MVVM pattern to improve testability and code reuse. I have since applied this lesson to my subsequent **Wordle** project, where I prioritized a clean MVC split from day one.
* **Asynchronous Processing**: To ensure a smooth user experience, I utilized multi-threading to handle resource-intensive tasks, such as loading map tile layers and fetching live API data, without blocking the main UI thread.
* **Data Persistence**: I used **SQLite** to manage user accounts and local storage for journey history, ensuring data remains persistent across sessions.
* **Testing and Evaluation**: The project underwent rigorous user testing, including recorded interviews to validate that the pathfinding and fare calculation met real-world user needs.

## Key Features

* **Advanced Pathfinding**: Rapidly calculates the most efficient route between any two Underground stations using the A* algorithm.
* **Live TfL Data**: Integrates with the TfL Unified API to provide real-time updates on line status and train timings.
* **Interactive Geographic Map**: Features a map built with OpenStreetMap (OSM) that dynamically plots your journey.
* **Dynamic Fare Calculation**: Automatically calculates the cost of a journey,

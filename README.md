# Image Similarity Algorithm

## Features
- Load a folder of images (multiple formats supported: `.jpg`, `.jpeg`, `.png`, `.bmp`, `.gif`, `.tiff`, `.ico`).
- Quickly compute histogram information (min, max, median, mean, std. dev, histogram array) for all images.
- Display histograms (R, G, B channels) for the currently selected image.
- Select a query image, compute its histogram, and retrieve the top N matches using cosine distance.
- Visualize the matching results along with exact distance scores.

## Technologies Used
- **C# WinForms**
- **.NET Framework** 
- `System.Windows.Forms.DataVisualization` for histogram charts
- Basic image processing & statistics

## Getting Started
### Clone this repository:
```sh
git clone https://github.com/AbanoubPhelopos/Image-Similarity-Algorithm.git
```

1. Open the solution in **Visual Studio** (or your preferred C# IDE).
2. Build the solution.
3. Run the application.

Alternatively, you can create a new **Visual Studio project** and include these source files.

## Structure Overview
Key classes and files:

### `MainForm.cs`
- Contains the main GUI logic: buttons, event handlers, form controls.

### `ImageHistSimilarity.cs`
- Houses the main logic for calculating image statistics and matching.
- Three important methods:
  - `CalculateImageStats()`
  - `LoadAllImages()`
  - `FindTopMatches()`

### `ImageOperations.cs`
- Low-level image load/display utilities and histogram drawing.

### `Program.cs`
- Standard entry point for the WinForms application.

## Usage
1. Click **"Load Images"** to select a folder containing your images.
2. The application will parse all images in that folder and build their histograms.
3. Click **"Open"** to select a query image.
4. Enter the number of top matches (**N**).
5. Click **"Match Image"** to compute and display the N most similar images.
6. Selecting an image in the "Matched Images" list shows its histogram and the match score.

## How It Works
### Histogram Calculation
For each image, three histograms of length **256** are computed (one for each color channel: **R, G, B**). Each histogram bin represents how many pixels have an intensity at that value (**0–255**).

Additionally, we compute:
- **Minimum, Maximum intensity**
- **Median intensity**
- **Mean intensity**
- **Standard Deviation**

### Cosine Distance Similarity
The cosine distance (or angle) is calculated between the query’s color histogram and the target’s color histogram:

\[ \text{distance} = \arccos \left( \frac{Q \cdot T}{||Q|| \times ||T||} \right) \]

where **Q, T** are 256-dimensional normalized vectors for each color channel (i.e., probabilities). We compute this angle for each channel, then average them (**R, G, B**) to get a final score. **Lower angles indicate higher similarity.**

const plasma = [[0,"#0d0887"],[0.2,"#6a00a8"],[0.4,"#b12a90"],[0.6,"#e16462"],[0.8,"#fca636"],[1,"#f0f921"]];

/** Render the notebook-compatible geographic path and three-dimensional ribbon. */
export async function renderFlightPath(mapId, sceneId, sceneLoadingId, path, colourMode, groundUrl) {
    // Blazor supplies renderer-neutral DTOs; this adapter performs browser rendering only.
    if (!window.Plotly) throw new Error("Plotly.js could not be loaded.");
    setSceneLoading(sceneId, sceneLoadingId, true);
    try {
    const points = path.points ?? [];
    const colours = selectColourValues(points, colourMode);
    const colourTitle = colourMode === "time" ? "Elapsed (s)" : colourMode === "distance" ? "Distance (nm)" : "Altitude (m)";
    const segments = groupSegments(points);
    const mapTraces = [];

    for (const segment of segments) {
        // Separate traces prevent straight lines from bridging notebook-compatible 90-second gaps.
        mapTraces.push({type:"scatter",mode:"lines+markers",x:segment.map(p=>p.longitude),y:segment.map(p=>p.latitude),
            line:{color:"#35d4d0",width:3},marker:{size:6,color:segment.map(p=>colours[p.sequence-1]),colorscale:plasma,
            cmin:Math.min(...colours),cmax:Math.max(...colours),showscale:mapTraces.length===0,colorbar:{title:colourTitle,thickness:12}},
            customdata:segment.map(hoverData),hovertemplate:hoverTemplate(),name:`Segment ${segment[0].segment}`});
    }
    addEndpointTraces(mapTraces, points, false);
    if (path.receiverLatitude != null && path.receiverLongitude != null) {
        // The receiver marker anchors both geographic distance and bearing measurements.
        mapTraces.push({type:"scatter",mode:"markers",x:[path.receiverLongitude],y:[path.receiverLatitude],
            marker:{size:11,symbol:"diamond",color:"#fff",line:{color:"#07111f",width:2}},hovertemplate:"Receiver<extra></extra>",name:"Receiver"});
    }
    const images = groundUrl ? [{source:groundUrl,xref:"x",yref:"y",x:path.west,y:path.north,
        sizex:path.east-path.west,sizey:path.north-path.south,sizing:"stretch",opacity:0.95,layer:"below"}] : [];
    await window.Plotly.react(mapId,mapTraces,{paper_bgcolor:"#0d1b2d",plot_bgcolor:"#101b27",font:{color:"#b8cada"},
        margin:{l:55,r:30,t:25,b:50},xaxis:{title:"Longitude",range:[path.west,path.east],gridcolor:"#213750",zeroline:false},
        yaxis:{title:"Latitude",range:[path.south,path.north],gridcolor:"#213750",zeroline:false,scaleanchor:"x",scaleratio:1},
        images,legend:notebookLegend(),hovermode:"closest"},plotConfig());

    const sceneTraces=[];
    for (const segment of segments) {
        if(segment.length<2) continue;
        const ribbon=curtainSurface(segment,colours,path.minimumAltitudeMetres);
        sceneTraces.push({type:"surface",...ribbon,colorscale:plasma,opacity:0.85,name:`Ribbon ${segment[0].segment}`,
            showscale:sceneTraces.length===0,colorbar:{title:colourTitle,x:1.05},lighting:{ambient:0.6,diffuse:0.8,specular:0.3,roughness:0.5,fresnel:0.2},
            lightposition:{x:2000,y:0,z:8000},hoverinfo:"skip"});
        sceneTraces.push({type:"scatter3d",mode:"lines",x:segment.map(p=>p.localXMetres),y:segment.map(p=>p.localYMetres),
            z:segment.map(p=>p.altitudeMetres),line:{color:"#fff",width:4},name:`Flight path ${segment[0].segment}`,
            customdata:segment.map(hoverData),hovertemplate:hoverTemplate()});
        sceneTraces.push({type:"scatter3d",mode:"lines",x:segment.map(p=>p.localXMetres),y:segment.map(p=>p.localYMetres),
            z:segment.map(()=>path.minimumAltitudeMetres),line:{color:"#8fa6be",width:2,dash:"dash"},name:`Ground trace ${segment[0].segment}`,hoverinfo:"skip"});
    }
    addEndpointTraces(sceneTraces,points,true);
    if(groundUrl){const ground=await createGroundMesh(groundUrl,points,Math.max(0,path.minimumAltitudeMetres));if(ground)sceneTraces.push(ground);}
    await window.Plotly.react(sceneId,sceneTraces,{paper_bgcolor:"#0d1b2d",font:{color:"#b8cada"},margin:{l:20,r:30,t:20,b:20},
        scene:{bgcolor:"#101b27",aspectmode:"auto",xaxis:{title:"East / West (m)",gridcolor:"#213750"},
        yaxis:{title:"North / South (m)",gridcolor:"#213750"},zaxis:{title:"Altitude (m)",range:[path.minimumAltitudeMetres,path.maximumAltitudeMetres],gridcolor:"#213750",autorange:false}},
        legend:notebookLegend()},plotConfig());
    } finally {
        // Always remove the busy state, including when Plotly reports a rendering failure.
        setSceneLoading(sceneId, sceneLoadingId, false);
    }
}

/** Show or hide the three-dimensional rendering progress indicator. */
function setSceneLoading(sceneId, sceneLoadingId, isLoading){
    // Update native accessibility state alongside the visible overlay.
    const scene=document.getElementById(sceneId),indicator=document.getElementById(sceneLoadingId);
    if(scene)scene.setAttribute("aria-busy",isLoading?"true":"false");
    if(indicator)indicator.classList.toggle("hidden",!isLoading);
}

/** Release Plotly resources when the Blazor component is removed. */
export function disposeFlightPath(mapId,sceneId){
    // Purging removes Plotly event handlers and WebGL contexts.
    if(window.Plotly){window.Plotly.purge(mapId);window.Plotly.purge(sceneId);}
}

/** Group chronologically prepared points by their C# segment identifier. */
function groupSegments(points){
    // A map preserves the input order of the already sorted DTO sequence.
    return [...points.reduce((groups,p)=>{if(!groups.has(p.segment))groups.set(p.segment,[]);groups.get(p.segment).push(p);return groups;},new Map()).values()];
}

/** Select numeric colour values for the requested display mode. */
function selectColourValues(points,mode){
    // Time is relative to the first valid point, matching the profile chart convention.
    const first=points.length?new Date(points[0].timestamp).getTime():0;
    return points.map(p=>mode==="time"?(new Date(p.timestamp).getTime()-first)/1000:mode==="distance"?p.distanceNauticalMiles:p.altitudeMetres);
}

/** Build Plotly custom hover values from one path point. */
function hoverData(p){
    // Keep formatting in the contained adapter while preserving the full C# DTO.
    return [p.sequence,p.timestamp,p.altitudeFeet,p.altitudeMetres,p.distanceNauticalMiles,p.bearingDegrees,p.latitude,p.longitude];
}

/** Return the common geographic and 3D point hover template. */
function hoverTemplate(){
    // Plotly substitutes customdata without exposing implementation details to components.
    return "Point %{customdata[0]}<br>%{customdata[1]}<br>Altitude %{customdata[2]:.0f} ft (%{customdata[3]:.0f} m)<br>Distance %{customdata[4]:.1f} nm<br>Bearing %{customdata[5]:.0f}°<br>%{customdata[6]:.5f}, %{customdata[7]:.5f}<extra></extra>";
}

/** Build a two-row Plotly surface from the aircraft path down to the visible chart floor. */
function curtainSurface(segment,colours,zFloor){
    const pathX=segment.map(p=>p.localXMetres),pathY=segment.map(p=>p.localYMetres);
    const pathZ=segment.map(p=>p.altitudeMetres),floorZ=segment.map(()=>zFloor);
    const pathColours=segment.map(p=>colours[p.sequence-1]);
    return{x:[pathX,pathX],y:[pathY,pathY],z:[pathZ,floorZ],surfacecolor:[pathColours,pathColours],
        cmin:Math.min(...colours),cmax:Math.max(...colours)};
}

/** Add explicit start and end markers to either plot dimension. */
function addEndpointTraces(traces,points,is3d){
    if(!points.length)return;
    // Separate traces produce clear markers that do not depend on the selected colour scale.
    [[points[0],"Start","#4fdaa1"],[points.at(-1),"End","#ff7186"]].forEach(([p,name,colour])=>{
        const trace={type:is3d?"scatter3d":"scatter",mode:"markers",name,x:[is3d?p.localXMetres:p.longitude],y:[is3d?p.localYMetres:p.latitude],
            marker:{size:is3d?6:10,color:colour,line:{color:"#07111f",width:2}},customdata:[hoverData(p)],hovertemplate:hoverTemplate()};
        if(is3d)trace.z=[p.altitudeMetres];traces.push(trace);
    });
}

/** Decode and down-sample a Mapbox PNG into a Plotly Mesh3d floor texture. */
async function createGroundMesh(url,points,zFloor){
    try{
        const response=await fetch(url);if(!response.ok)return null;
        // Match the notebook's 512-pixel texture mesh so map labels and geographic detail remain legible.
        const bitmap=await createImageBitmap(await response.blob());const maxPx=512,scale=Math.min(1,maxPx/Math.max(bitmap.width,bitmap.height));
        const width=Math.max(2,Math.round(bitmap.width*scale)),height=Math.max(2,Math.round(bitmap.height*scale));
        const canvas=document.createElement("canvas");canvas.width=width;canvas.height=height;const context=canvas.getContext("2d",{willReadFrequently:true});
        // High-quality smoothing is the browser equivalent of the notebook's Lanczos resize step.
        context.imageSmoothingEnabled=true;context.imageSmoothingQuality="high";
        context.drawImage(bitmap,0,0,width,height);const pixels=context.getImageData(0,0,width,height).data;
        const minX=Math.min(...points.map(p=>p.localXMetres)),maxX=Math.max(...points.map(p=>p.localXMetres));
        const minY=Math.min(...points.map(p=>p.localYMetres)),maxY=Math.max(...points.map(p=>p.localYMetres));
        const x=[],y=[],z=[],vertexcolor=[],i=[],j=[],k=[];
        // Map rows run north-to-south, so invert local Y while generating floor vertices.
        for(let row=0;row<height;row++)for(let col=0;col<width;col++){x.push(minX+(maxX-minX)*col/(width-1));y.push(maxY-(maxY-minY)*row/(height-1));z.push(zFloor);
            // Retain the source colour at every vertex, exactly as the notebook implementation does.
            const px=(row*width+col)*4;vertexcolor.push(`rgb(${pixels[px]},${pixels[px+1]},${pixels[px+2]})`);}
        for(let row=0;row<height-1;row++)for(let col=0;col<width-1;col++){const tl=row*width+col;i.push(tl,tl);j.push(tl+width,tl+width+1);k.push(tl+width+1,tl+1);}
        return{type:"mesh3d",x,y,z,i,j,k,vertexcolor,flatshading:true,showscale:false,lighting:{ambient:0.9,diffuse:0.2},hoverinfo:"skip",opacity:1,name:"Map"};
    }catch{
        // Image decoding, networking, or WebGL limitations all use the supported blank floor.
        return null;
    }
}

/** Return notebook-aligned legend placement. */
function notebookLegend(){
    // A translucent legend remains readable over either blank or textured ground.
    return{x:0.02,y:0.98,bgcolor:"rgba(7,17,31,0.72)",bordercolor:"rgba(143,166,190,0.25)",borderwidth:1};
}

/** Return shared Plotly interaction settings. */
function plotConfig(){
    // Responsive rendering supports zoom and resize without a Blazor page reload.
    return{responsive:true,displaylogo:false,scrollZoom:true};
}

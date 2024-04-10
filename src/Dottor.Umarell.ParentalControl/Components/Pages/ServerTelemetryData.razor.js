export function initMap(el, lat, lon) {
    var map = L.map(el, { attributionControl: false })
        .setView([lat, lon], 15);

    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        //attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    }).addTo(map);

    L.circle([lat, lon], { radius: 700 }).addTo(map);

    return map;
}

export function createPolyline(map, lat, lon) {
    var myPolyline = L.polyline([
        [lat, lon],
        [lat, lon]
    ], { color: '#D6001C', weight: 5 });
    myPolyline.addTo(map);

    var umarellIcon = L.icon({ iconUrl: 'images/umarell_marker.png', iconSize: [50, 100], iconAnchor: [25, 99] });
    var marker = L.marker([lat, lon], { icon: umarellIcon });
    marker.addTo(map);
    // HACK: store marker in global variable
    window.umarellMarker = marker;

    return myPolyline;
};

export function updateMap(map, polyline, lat, lon) {
    polyline.addLatLng([lat, lon]);

    window.umarellMarker.setLatLng([lat, lon]);

    map.panTo([lat, lon]).update();
}
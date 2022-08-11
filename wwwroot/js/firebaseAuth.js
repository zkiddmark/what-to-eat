
  // Import the functions you need from the SDKs you need
import { initializeApp } from "https://www.gstatic.com/firebasejs/9.9.2/firebase-app.js";
import { getAuth, signInWithEmailAndPassword, signOut, onAuthStateChanged } from "https://www.gstatic.com/firebasejs/9.9.2/firebase-auth.js";
  // TODO: Add SDKs for Firebase products that you want to use
  // https://firebase.google.com/docs/web/setup#available-libraries

  // Your web app's Firebase configuration gets loaded from an external file. See Program.cs

// Initialize Firebase.
let app, auth;

export async function initializeFirebase(config) {
    const options = JSON.parse(config);
    if (!app) {
        app = initializeApp(options);
        auth = getAuth(app);
    }
    
    auth.onAuthStateChanged((user) => {
        user 
            ? localStorage.setItem('user', JSON.stringify(user))
            : localStorage.removeItem('user');

    });

    const currentUser = await getCurrentlyLoggedOnUser()
    return currentUser;
}

export async function getCurrentlyLoggedOnUser() {
    return JSON.parse(localStorage.getItem('user'));
}

export async function signIn(email, password) {
    const user = await signInWithEmailAndPassword(auth, email, password);
    return user.user;
}

export async function logout() {
    await signOut(auth);
}
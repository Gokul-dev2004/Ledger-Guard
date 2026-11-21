import express from 'express';
import { 
    registerUser, 
    loginUser, 
    getProfile,
    forgotPassword,
    verifyOTP,
    resetPassword } from '../controllers/authController.js';
import authMiddleware from '../middleware/authMiddleware.js';

const router = express.Router();

router.post('/register', registerUser);
router.post('/login', loginUser);
router.get('/profile', authMiddleware, getProfile)
router.post('/forgot-password', forgotPassword);
router.post('/verify-otp', verifyOTP);
router.post('/reset-password', resetPassword)

export default router;